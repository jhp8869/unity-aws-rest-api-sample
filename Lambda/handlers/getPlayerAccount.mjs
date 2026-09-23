import Ajv from "ajv";
import { ok, fail, ErrorCode } from "../shared/apiResponse.mjs";
import { loadAccount } from "../shared/repositories.mjs";
import { getAuthorizedPlayerId, parseBody } from "../shared/request.mjs";

const validate = new Ajv().compile({
  type: "object",
  required: ["PlayerId"],
  properties: { PlayerId: { type: "string", minLength: 1 } },
  additionalProperties: false
});

export const handler = async (event) => {
  const body = parseBody(event);
  const playerId = getAuthorizedPlayerId(event, body);
  const request = { ...body, PlayerId: playerId };

  if (!validate(request)) return fail(ErrorCode.InvalidInput, "Invalid request format");
  const account = await loadAccount(playerId);
  if (!account) return fail(ErrorCode.AccountNotFound, "Account not found");

  delete account.Salt;
  delete account.Password;
  return ok(account);
};
