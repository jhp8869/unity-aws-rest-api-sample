import { CognitoIdentityProviderClient } from "@aws-sdk/client-cognito-identity-provider";
import Ajv from "ajv";
import { ok, fail, ErrorCode } from "../shared/apiResponse.mjs";
import { createLogger } from "../shared/logger.mjs";
import { exchangeCustomCredentials, loginPlayer, signUpPlayer } from "../shared/authService.mjs";
import { parseBody } from "../shared/request.mjs";

const cognito = new CognitoIdentityProviderClient({ region: process.env.AWS_REGION ?? "ap-northeast-2" });
const validate = new Ajv().compile({
  type: "object",
  required: ["customId", "customPw", "register", "Timezone"],
  properties: {
    customId: { type: "string", minLength: 1 },
    customPw: { type: "string", minLength: 1 },
    register: { type: "boolean" },
    Timezone: { type: "string", pattern: "^[+-][0-9]{2}:[0-9]{2}$" }
  },
  additionalProperties: false
});

export const handler = async (event) => {
  const body = parseBody(event);
  const logger = createLogger();

  if (!validate(body)) {
    logger.warn("Invalid custom login request", { errors: validate.errors });
    return fail(ErrorCode.InvalidInput, "Invalid request format");
  }

  try {
    const customPlayerId = await exchangeCustomCredentials(body.customId, body.customPw);
    try {
      return ok(await loginPlayer(cognito, customPlayerId));
    } catch {
      if (body.register !== true) return fail(ErrorCode.AccountNotFound, "Account is not registered");
      return ok(await signUpPlayer(cognito, customPlayerId, body.Timezone));
    }
  } catch (error) {
    logger.error("CustomLogin failed", error);
    return fail(ErrorCode.InternalServerError, "Internal server error");
  }
};
