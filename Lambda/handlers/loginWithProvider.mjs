import { CognitoIdentityProviderClient } from "@aws-sdk/client-cognito-identity-provider";
import Ajv from "ajv";
import { ok, fail, ErrorCode } from "../shared/apiResponse.mjs";
import { createLogger } from "../shared/logger.mjs";
import {
  exchangeProviderCode,
  loginPlayer,
  signUpPlayer
} from "../shared/authService.mjs";

const cognito = new CognitoIdentityProviderClient({
  region: process.env.AWS_REGION ?? "ap-northeast-2"
});

const ajv = new Ajv();

const schema = {
  type: "object",
  required: ["authorizationCode", "register"],
  properties: {
    authorizationCode: { type: "string", minLength: 1 },
    timezoneOffset: { type: "string" },
    register: { type: "boolean" }
  }
};

const validate = ajv.compile(schema);

export const handler = async (event) => {
  const body = typeof event.body === "string"
    ? JSON.parse(event.body)
    : event.body;

  const logger = createLogger();

  if (!validate(body)) {
    logger.warn("Invalid login request", { errors: validate.errors });
    return fail(ErrorCode.InvalidInput, "Invalid request format");
  }

  try {
    const providerPlayerId = await exchangeProviderCode(body.authorizationCode);

    try {
      const session = await loginPlayer(cognito, providerPlayerId);
      return ok(session);
    } catch {
      if (body.register !== true) {
        return fail(ErrorCode.AccountNotFound, "Account is not registered");
      }

      const session = await signUpPlayer(
        cognito,
        providerPlayerId,
        body.timezoneOffset
      );

      return ok(session);
    }
  } catch (error) {
    logger.error("LoginWithProvider failed", error);
    return fail(ErrorCode.InternalServerError, "Internal server error");
  }
};
