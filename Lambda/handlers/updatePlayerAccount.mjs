import Ajv from "ajv";
import { ok, fail, ErrorCode } from "../shared/apiResponse.mjs";
import { loadAccount, updateAccount } from "../shared/repositories.mjs";
import { getAuthorizedPlayerId, parseBody } from "../shared/request.mjs";

const validate = new Ajv().compile({
  type: "object",
  required: ["PlayerId"],
  properties: {
    PlayerId: { type: "string", minLength: 1 },
    Agreement: { type: "object" },
    SelectServer: { type: "object" },
    DeleteAccount: { anyOf: [{ type: "object" }, { type: "null" }] },
    SNS: { type: "object" },
    DeviceInfo: { type: "object" }
  },
  additionalProperties: false
});

const allowedKeys = new Set(["Agreement", "SelectServer", "DeleteAccount", "SNS", "DeviceInfo"]);

export const handler = async (event) => {
  const body = parseBody(event);
  const playerId = getAuthorizedPlayerId(event, body);
  const request = { ...body, PlayerId: playerId };

  if (!validate(request)) return fail(ErrorCode.InvalidInput, "Invalid request format");
  if (!await loadAccount(playerId)) return fail(ErrorCode.AccountNotFound, "Account not found");

  const patch = Object.fromEntries(
    Object.entries(request).filter(([key]) => allowedKeys.has(key))
  );

  if (patch.DeleteAccount && typeof patch.DeleteAccount === "object") {
    patch.DeleteAccount = patch.DeleteAccount.Date?.trim()
      ? Math.floor(Date.now() / 1000) + 30 * 24 * 60 * 60
      : null;
  }

  await updateAccount(playerId, patch);
  return ok({});
};
