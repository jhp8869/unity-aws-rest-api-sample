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
import {
  applyInAppPurchaseReward,
  findInAppPurchaseItem
} from "../shared/iapRewardService.mjs";
import { verifyGooglePurchase } from "../shared/storeVerification.mjs";

const ajv = new Ajv();
const validate = ajv.compile({
  type: "object",
  required: ["PlayerId", "ReceiptJson"],
  properties: {
    PlayerId: { type: "string", minLength: 1 },
    ReceiptJson: { type: "string", minLength: 1 },
    Signature: { type: "string" },
    CurrencyCode: { type: "string" },
    Price: { type: "number" }
  }
});

export const handler = async (event) => {
  const body = typeof event.body === "string" ? JSON.parse(event.body) : event.body;

  if (event.requestContext?.authorizer?.claims) {
    body.PlayerId = event.requestContext.authorizer.claims["cognito:username"];
  }

  const logger = createLogger(body.PlayerId);

  if (!validate(body)) {
    logger.warn("Invalid Google purchase validation request", { errors: validate.errors });
    return fail(ErrorCode.InvalidInput, "Invalid request format");
  }

  try {
    const receipt = JSON.parse(body.ReceiptJson);

    if (!receipt.packageName || !receipt.productId || !receipt.purchaseToken) {
      return fail(ErrorCode.InvalidInput, "Invalid Google receipt format");
    }

    await verifyGooglePurchase({
      packageName: receipt.packageName,
      productId: receipt.productId,
      purchaseToken: receipt.purchaseToken
    });

    return grantVerifiedPurchase(body.PlayerId, receipt.productId);
  } catch (error) {
    logger.error("ValidateGooglePurchase failed", error);
    return fail(ErrorCode.InternalServerError, "Purchase validation failed");
  }
};

async function grantVerifiedPurchase(playerId, productId) {
  const account = await loadAccount(playerId, ["SelectedServer", "Timezone", "BanInfo"]);

  if (!account?.SelectedServer) {
    return fail(ErrorCode.InvalidInput, "Player has not selected a server");
  }

  if (account.BanInfo?.Active === true) {
    return fail(ErrorCode.BannedPlayer, "Player account is banned");
  }

  const [playerData, shopTable, itemTable] = await Promise.all([
    loadPlayerData(playerId, account.SelectedServer, ["Inventory", "ShopStore"]),
    loadShopTable("InAppPurchase"),
    loadItemTable()
  ]);

  const found = findInAppPurchaseItem(shopTable, productId);

  if (!playerData || !found?.shopItem?.isInAppPurchase) {
    return fail(ErrorCode.InvalidInput, "Invalid in-app purchase item");
  }

  const reward = applyInAppPurchaseReward({
    playerData,
    itemTable,
    shopId: found.shopId,
    productId,
    shopItem: found.shopItem
  });

  if (!reward.ok) {
    return fail(ErrorCode.InvalidInput, reward.reason);
  }

  await savePlayerData(playerId, account.SelectedServer, playerData, ["Inventory", "ShopStore"]);
  return ok(reward);
}
