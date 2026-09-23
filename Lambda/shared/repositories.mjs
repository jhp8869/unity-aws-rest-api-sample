import { DynamoDBClient } from "@aws-sdk/client-dynamodb";
import { DynamoDBDocumentClient, GetCommand, UpdateCommand } from "@aws-sdk/lib-dynamodb";
import { S3Client, GetObjectCommand } from "@aws-sdk/client-s3";

const region = process.env.AWS_REGION ?? "ap-northeast-2";
const tablePrefix = process.env.TABLE_PREFIX ?? "sample";
const bucketName = process.env.CONTENT_BUCKET ?? "sample-content-bucket";

const dynamodb = DynamoDBDocumentClient.from(new DynamoDBClient({ region }));
const s3 = new S3Client({ region });

/** savePlayerData 의 낙관적 락 충돌. 핸들러는 이를 ErrorCode.Conflict 로 변환한다. */
export class ConcurrentModificationError extends Error {
  constructor(playerId) {
    super(`Player data was modified concurrently: ${playerId}`);
    this.name = "ConcurrentModificationError";
  }
}

export async function loadAccount(playerId, keys = []) {
  const result = await dynamodb.send(new GetCommand({
    TableName: `${tablePrefix}_Account`,
    Key: { PlayerId: playerId }
  }));

  if (!result.Item) {
    return null;
  }

  return pickKeys(result.Item, keys);
}

export async function updateAccount(playerId, patch) {
  const entries = Object.entries(patch ?? {}).filter(([, value]) => value !== undefined);
  if (entries.length === 0) return loadAccount(playerId);

  const names = {};
  const values = {};
  const assignments = [];

  for (const [key, value] of entries) {
    names[`#${key}`] = key;
    values[`:${key}`] = value;
    assignments.push(`#${key} = :${key}`);
  }

  await dynamodb.send(new UpdateCommand({
    TableName: `${tablePrefix}_Account`,
    Key: { PlayerId: playerId },
    UpdateExpression: `SET ${assignments.join(", ")}`,
    ExpressionAttributeNames: names,
    ExpressionAttributeValues: values
  }));

  return loadAccount(playerId);
}

/**
 * 플레이어 데이터를 읽는다. 요청한 keys 와 별도로 항상 `Version` 을 포함해
 * 이후 savePlayerData 의 낙관적 락 조건으로 쓴다.
 */
export async function loadPlayerData(playerId, server, keys = []) {
  const result = await dynamodb.send(new GetCommand({
    TableName: `${tablePrefix}_PlayerData`,
    Key: {
      ServerId: server.id,
      PlayerId: playerId
    }
  }));

  if (!result.Item) {
    return null;
  }

  const picked = pickKeys(result.Item, keys);
  picked.Version = result.Item.Version ?? 0;
  return picked;
}

/**
 * 읽었을 때의 Version 과 같을 때만 저장한다 (낙관적 락).
 *
 * Lambda 는 요청마다 병렬 실행되므로, 같은 플레이어의 두 요청이 같은 스냅샷을 읽고
 * 각자 저장하면 나중 저장이 앞의 변경을 덮어쓴다 (예: 구매 두 번 → 재화 한 번만 차감).
 * ConditionExpression 으로 DynamoDB 가 이를 거부하게 하고, 핸들러는 Conflict 를 돌려준다.
 */
export async function savePlayerData(playerId, server, data, keys) {
  const names = { "#Version": "Version" };
  const values = { ":expectedVersion": data.Version ?? 0, ":nextVersion": (data.Version ?? 0) + 1 };
  const assignments = ["#Version = :nextVersion"];

  for (const key of keys) {
    names[`#${key}`] = key;
    values[`:${key}`] = data[key];
    assignments.push(`#${key} = :${key}`);
  }

  try {
    await dynamodb.send(new UpdateCommand({
      TableName: `${tablePrefix}_PlayerData`,
      Key: {
        ServerId: server.id,
        PlayerId: playerId
      },
      UpdateExpression: `SET ${assignments.join(", ")}`,
      ConditionExpression: "attribute_not_exists(#Version) OR #Version = :expectedVersion",
      ExpressionAttributeNames: names,
      ExpressionAttributeValues: values
    }));
    data.Version = values[":nextVersion"];
  } catch (error) {
    if (error?.name === "ConditionalCheckFailedException") {
      throw new ConcurrentModificationError(playerId);
    }
    throw error;
  }
}

export async function loadShopTable(shopId) {
  return readJsonObject(`common/Shop/${shopId}.json`);
}

export async function loadItemTable() {
  return readJsonObject("common/Item/ItemTable.json");
}

export async function loadServerList() {
  return readJsonObject(
    process.env.SERVER_LIST_KEY ?? "common/ServerList/v1/ServerList.json",
    parseJsonEnv("SERVER_LIST_JSON", { DevServer: { id: "DevServer", name: "개발서버" } })
  );
}

export async function loadServerState() {
  return readJsonObject(
    process.env.SERVER_STATE_KEY ?? "common/ServerState/v1/ServerState.json",
    parseJsonEnv("SERVER_STATE_JSON", {})
  );
}

async function readJsonObject(key, fallback) {
  try {
    const result = await s3.send(new GetObjectCommand({
      Bucket: bucketName,
      Key: key
    }));

    const text = await result.Body.transformToString();
    return JSON.parse(text);
  } catch (error) {
    if (fallback !== undefined && (error?.name === "NoSuchKey" || error?.$metadata?.httpStatusCode === 404)) {
      return fallback;
    }
    throw error;
  }
}

function parseJsonEnv(name, fallback) {
  try {
    return process.env[name] ? JSON.parse(process.env[name]) : fallback;
  } catch {
    return fallback;
  }
}

function pickKeys(source, keys) {
  if (keys.length === 0) {
    return { ...source };
  }

  return Object.fromEntries(keys.map(key => [key, source[key]]));
}
