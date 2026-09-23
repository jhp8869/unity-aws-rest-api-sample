import { ok, fail, ErrorCode } from "../shared/apiResponse.mjs";
import { loadAccount, loadPlayerData } from "../shared/repositories.mjs";
import { getAuthorizedPlayerId, parseBody } from "../shared/request.mjs";

export const handler = async (event) => {
  const body = parseBody(event);
  const playerId = getAuthorizedPlayerId(event, body);
  const account = await loadAccount(playerId, ["SelectServer"]);

  if (!account) return fail(ErrorCode.AccountNotFound, "Account not found");
  if (!account.SelectServer?.id) return fail(ErrorCode.InvalidInput, "Player has not selected a server");

  const playerInfo = await loadPlayerData(playerId, account.SelectServer);
  return ok(playerInfo ?? { PlayerId: playerId, ServerId: account.SelectServer.id });
};
