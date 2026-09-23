import { ok, fail, ErrorCode } from "../shared/apiResponse.mjs";
import { loadServerList, loadServerState } from "../shared/repositories.mjs";

export const handler = async (event) => {
  try {
    const requestData = typeof event?.body === "string"
      ? JSON.parse(event.body)?.RequestData
      : event?.body?.RequestData;

    const result = {};
    if (!requestData || requestData.ServerList) result.ServerList = await loadServerList();
    if (!requestData || requestData.ServerState) result.ServerState = await loadServerState();
    return ok(result);
  } catch (error) {
    return fail(ErrorCode.InternalServerError, error.message);
  }
};
