export function ensureShopStoreRecord(shopStore, shopId, productId) {
  shopStore[shopId] ??= {};
  shopStore[shopId][productId] ??= {
    totalPurchaseCount: 0,
    dailyPurchaseCount: 0,
    firstPurchaseTime: null,
    lastPurchaseTime: null,
    lastDailyRewardDate: null
  };

  return shopStore[shopId][productId];
}

export function validatePurchaseLimit(shopItem, record, now = new Date()) {
  const settings = shopItem.purchaseLimitSettings;

  if (!settings?.hasPurchaseLimit) {
    return { ok: true };
  }

  const limit = Number(settings.purchaseLimitCount);

  if (settings.limitType === "Permanent") {
    return record.totalPurchaseCount >= limit
      ? { ok: false, reason: "Total purchase limit exceeded" }
      : { ok: true };
  }

  if (!isSameLimitWindow(record.lastPurchaseTime, now, settings.limitType)) {
    record.dailyPurchaseCount = 0;
  }

  return record.dailyPurchaseCount >= limit
    ? { ok: false, reason: `${settings.limitType} purchase limit exceeded` }
    : { ok: true };
}

export function markPurchased(record, now = new Date()) {
  record.totalPurchaseCount++;
  record.dailyPurchaseCount++;
  record.firstPurchaseTime ??= now.toISOString();
  record.lastPurchaseTime = now.toISOString();
}

function isSameLimitWindow(lastPurchaseTime, now, limitType) {
  if (!lastPurchaseTime) {
    return false;
  }

  const last = new Date(lastPurchaseTime);

  if (limitType === "Daily") {
    return last.toISOString().slice(0, 10) === now.toISOString().slice(0, 10);
  }

  if (limitType === "Monthly") {
    return last.getUTCFullYear() === now.getUTCFullYear()
      && last.getUTCMonth() === now.getUTCMonth();
  }

  if (limitType === "Weekly") {
    const msPerWeek = 7 * 24 * 60 * 60 * 1000;
    return Math.floor(last.getTime() / msPerWeek) === Math.floor(now.getTime() / msPerWeek);
  }

  return false;
}
