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
    playerId: username,
    idToken: auth.IdToken,
    accessToken: auth.AccessToken,
    refreshToken: auth.RefreshToken,
    tokenType: auth.TokenType
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
