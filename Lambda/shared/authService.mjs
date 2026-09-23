import crypto from "crypto";
import {
  AuthFlowType,
  InitiateAuthCommand,
  SignUpCommand
} from "@aws-sdk/client-cognito-identity-provider";

const clientId = process.env.COGNITO_CLIENT_ID;
const userPoolClientSecret = process.env.COGNITO_CLIENT_SECRET;
const playerHashSecret = process.env.PLAYER_HASH_SECRET;

export async function exchangeProviderCode(authorizationCode) {
  if (!authorizationCode) {
    throw new Error("authorizationCode is required");
  }

  // The production project exchanged this code with an external provider.
  // This portfolio version keeps only the architecture and removes provider secrets.
  return `provider:${hash(authorizationCode, playerHashSecret)}`;
}

export async function exchangeCustomCredentials(customId, customPw) {
  if (!customId || !customPw) {
    throw new Error("customId and customPw are required");
  }

  // 원본 CustomLogin의 Editor 전용 분기를 샘플 환경에 맞춘다.
  // 실제 운영 계정의 비밀번호와 Cognito 설정은 공개하지 않는다.
  return `custom:${hash(`${customId}:${customPw}`, playerHashSecret)}`;
}

export async function signUpPlayer(cognito, providerPlayerId, timezoneOffset) {
  const username = hash(providerPlayerId, playerHashSecret);
  const password = createInternalPassword(providerPlayerId);

  await cognito.send(new SignUpCommand({
    ClientId: clientId,
    SecretHash: createSecretHash(username),
    Username: username,
    Password: password,
    UserAttributes: [
      { Name: "custom:timezone", Value: timezoneOffset ?? "+00:00" }
    ]
  }));

  return loginWithPassword(cognito, username, password);
}

export async function loginPlayer(cognito, providerPlayerId) {
  const username = hash(providerPlayerId, playerHashSecret);
  const password = createInternalPassword(providerPlayerId);
  return loginWithPassword(cognito, username, password);
}

async function loginWithPassword(cognito, username, password) {
  const response = await cognito.send(new InitiateAuthCommand({
    ClientId: clientId,
    AuthFlow: AuthFlowType.USER_PASSWORD_AUTH,
    AuthParameters: {
      USERNAME: username,
      PASSWORD: password,
      SECRET_HASH: createSecretHash(username)
    }
  }));

  const auth = response.AuthenticationResult;

  return {
    PlayerId: username,
    IdToken: auth.IdToken,
    AccessToken: auth.AccessToken,
    RefreshToken: auth.RefreshToken,
    TokenType: auth.TokenType
  };
}

function createSecretHash(username) {
  return crypto
    .createHmac("sha256", userPoolClientSecret)
    .update(username + clientId)
    .digest("base64");
}

function createInternalPassword(providerPlayerId) {
  return `${hash(providerPlayerId, playerHashSecret)}Aa1!`;
}

function hash(value, secret) {
  return crypto
    .createHmac("sha256", secret)
    .update(value)
    .digest("hex");
}
