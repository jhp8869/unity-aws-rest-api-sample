import crypto from "crypto";
import { grantItem } from "./inventoryService.mjs";
import {
  ensureShopStoreRecord,
  markPurchased,
  validatePurchaseLimit
} from "./purchaseLimitService.mjs";

export function findInAppPurchaseItem(shopTable, productId) {
  for (const [shopId, shop] of Object.entries(shopTable)) {
    const shopItem = shop[productId];

    if (shopItem) {
      return { shopId, shopItem };
    }
  }

  return null;
}

export function applyInAppPurchaseReward({
  playerData,
  itemTable,
  shopId,
  productId,
  shopItem,
  now = new Date()
}) {
  playerData.Inventory ??= [];
  playerData.ShopStore ??= {};

  const record = ensureShopStoreRecord(playerData.ShopStore, shopId, productId);
  const limit = validatePurchaseLimit(shopItem, record, now);

  if (!limit.ok) {
    return { ok: false, reason: limit.reason };
  }

  const grants = [{ itemId: productId, amount: 1 }];

  if (shopItem.firstPurchaseSettings?.hasFirstPurchaseBonus && record.totalPurchaseCount === 0) {
    for (const bonus of shopItem.firstPurchaseSettings.bonusItems ?? []) {
      grants.push({ itemId: bonus.itemId, amount: bonus.amount });
    }
  }

  if (shopItem.bundleItemSettings?.hasBundleItems) {
    for (const bundle of shopItem.bundleItemSettings.bundleItems ?? []) {
      grants.push({ itemId: bundle.itemId, amount: bundle.amount });
    }
  }

  if (shopItem.dailyBundleItemSettings?.hasBundleItems) {
    for (const bundle of shopItem.dailyBundleItemSettings.bundleItems ?? []) {
      grants.push({ itemId: bundle.itemId, amount: bundle.amount });
    }

    record.lastDailyRewardDate = now.toISOString();
  }

  for (const randomTable of shopItem.randomPurchaseSettings?.randomTables ?? []) {
    for (let i = 0; i < randomTable.count; i++) {
      grants.push({
        itemId: selectRandomItem(randomTable.items, randomTable.relativeChances),
        amount: 1
      });
    }
  }

  const grantedItems = grants.map(grant => grantItem(playerData.Inventory, itemTable, grant));
  markPurchased(record, now);

  return {
    ok: true,
    purchaseItem: grantedItems.find(item => item.itemId === productId),
    Items: grantedItems,
    ConsumeItemResult: []
  };
}

function selectRandomItem(items, chances) {
  const total = chances.reduce((sum, chance) => sum + chance, 0);
  const roll = crypto.randomInt(0, total);
  let current = 0;

  for (let i = 0; i < items.length; i++) {
    current += chances[i];

    if (roll < current) {
      return items[i];
    }
  }

  return items[items.length - 1];
}
