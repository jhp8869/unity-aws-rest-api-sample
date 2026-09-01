import Ajv from "ajv";
import { ok, fail, ErrorCode } from "../shared/apiResponse.mjs";
import { createLogger } from "../shared/logger.mjs";
import {
  loadAccount,
  loadPlayerData,
  savePlayerData,
  loadShopTable,
  loadItemTable
} from "../shared/repositories.mjs";
import { consumeCurrency, grantItem } from "../shared/inventoryService.mjs";

const ajv = new Ajv();

const schema = {
  type: "object",
  required: ["PlayerId", "ShopId", "PurchaseItemIds"],
  properties: {
    PlayerId: { type: "string", minLength: 1 },
    ShopId: { type: "string", minLength: 1 },
    PurchaseItemIds: {
      type: "array",
      minItems: 1,
      items: { type: "string", minLength: 1 }
    }
  }
};

const validate = ajv.compile(schema);

export const handler = async (event) => {
  const body = typeof event.body === "string"
    ? JSON.parse(event.body)
    : event.body;

  if (event.requestContext?.authorizer?.claims) {
    body.PlayerId = event.requestContext.authorizer.claims["cognito:username"];
  }

  const logger = createLogger(body.PlayerId);

  if (!validate(body)) {
    logger.warn("Invalid purchase request", { errors: validate.errors });
    return fail(ErrorCode.InvalidInput, "Invalid request format");
  }

  try {
    const account = await loadAccount(body.PlayerId, ["SelectedServer", "Timezone", "BanInfo"]);

    if (!account?.SelectedServer) {
      return fail(ErrorCode.InvalidInput, "Player has not selected a server");
    }

    if (account.BanInfo?.Active === true) {
      return fail(ErrorCode.BannedPlayer, "Player account is banned");
    }

    const [playerData, shopTable, itemTable] = await Promise.all([
      loadPlayerData(body.PlayerId, account.SelectedServer, ["Wallet", "Inventory", "ShopStore"]),
      loadShopTable(body.ShopId),
      loadItemTable()
    ]);

    if (!playerData) {
      return fail(ErrorCode.InvalidInput, "Player data not found");
    }

    playerData.Wallet ??= {};
    playerData.Inventory ??= [];
    playerData.ShopStore ??= {};

    const purchaseItems = [];

    for (const purchaseItemId of body.PurchaseItemIds) {
      const shopItem = shopTable[purchaseItemId];

      if (!shopItem || shopItem.isInAppPurchase === true) {
        return fail(ErrorCode.InvalidInput, "Invalid shop item");
      }

      if (!consumeCurrency(playerData.Wallet, shopItem.currencyCode, shopItem.price)) {
        return fail(ErrorCode.LackResources, "Not enough currency");
      }

      const purchasedItem = grantItem(playerData.Inventory, itemTable, {
        itemId: shopItem.productItemId,
        amount: 1
      });

      const grantedItems = [];

      for (const reward of shopItem.rewards ?? []) {
        grantedItems.push(grantItem(playerData.Inventory, itemTable, {
          itemId: reward.itemId,
          amount: reward.amount
        }));
      }

      purchaseItems.push({
        purchasedItem,
        grantedItems,
        consumedItems: []
      });
    }

    await savePlayerData(body.PlayerId, account.SelectedServer, playerData, [
      "Wallet",
      "Inventory",
      "ShopStore"
    ]);

    return ok({ purchaseItems });
  } catch (error) {
    logger.error("PurchaseItem failed", error);
    return fail(ErrorCode.InternalServerError, "Internal server error");
  }
};
