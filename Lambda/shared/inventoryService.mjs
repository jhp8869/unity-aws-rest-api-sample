import crypto from "crypto";

export function grantItem(inventory, itemTable, grant) {
  const itemData = itemTable[grant.itemId];
  if (!itemData) {
    throw new Error(`Unknown item: ${grant.itemId}`);
  }

  const isStackable = itemData.stackable === true;

  if (isStackable) {
    const current = inventory.find(item => item.itemId === grant.itemId);
    if (current) {
      current.amount = String(BigInt(current.amount) + BigInt(grant.amount));
      return toSnapshot(current);
    }
  }

  const created = {
    itemInstanceId: crypto.randomUUID(),
    itemId: grant.itemId,
    amount: String(grant.amount)
  };

  inventory.push(created);
  return toSnapshot(created);
}

export function consumeCurrency(wallet, currencyCode, amount) {
  const current = BigInt(wallet[currencyCode] ?? 0);
  const cost = BigInt(amount);

  if (current < cost) {
    return false;
  }

  wallet[currencyCode] = (current - cost).toString();
  return true;
}

function toSnapshot(item) {
  return {
    itemInstanceId: item.itemInstanceId,
    itemId: item.itemId,
    amount: Number(item.amount)
  };
}
