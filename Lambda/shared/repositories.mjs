import { DynamoDBClient } from "@aws-sdk/client-dynamodb";
import { DynamoDBDocumentClient, GetCommand, UpdateCommand } from "@aws-sdk/lib-dynamodb";
import { S3Client, GetObjectCommand, PutObjectCommand } from "@aws-sdk/client-s3";

const region = process.env.AWS_REGION ?? "ap-northeast-2";
const tablePrefix = process.env.TABLE_PREFIX ?? "sample";
const bucketName = process.env.CONTENT_BUCKET ?? "sample-content-bucket";

const dynamodb = DynamoDBDocumentClient.from(new DynamoDBClient({ region }));
const s3 = new S3Client({ region });

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

  return pickKeys(result.Item, keys);
}

export async function savePlayerData(playerId, server, data, keys) {
  const names = {};
  const values = {};
  const assignments = [];

  for (const key of keys) {
    names[`#${key}`] = key;
    values[`:${key}`] = data[key];
    assignments.push(`#${key} = :${key}`);
  }

  await dynamodb.send(new UpdateCommand({
    TableName: `${tablePrefix}_PlayerData`,
    Key: {
      ServerId: server.id,
      PlayerId: playerId
    },
    UpdateExpression: `SET ${assignments.join(", ")}`,
    ExpressionAttributeNames: names,
    ExpressionAttributeValues: values
  }));
}

export async function loadShopTable(shopId) {
  return readJsonObject(`common/Shop/${shopId}.json`);
}

export async function loadItemTable() {
  return readJsonObject("common/Item/ItemTable.json");
}

async function readJsonObject(key) {
  const result = await s3.send(new GetObjectCommand({
    Bucket: bucketName,
    Key: key
  }));

  const text = await result.Body.transformToString();
  return JSON.parse(text);
}

function pickKeys(source, keys) {
  if (keys.length === 0) {
    return { ...source };
  }

  return Object.fromEntries(keys.map(key => [key, source[key]]));
}
