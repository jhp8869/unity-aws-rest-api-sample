export const ErrorCode = {
  Success: 0,
  InvalidInput: 1,
  NotAuthorized: 2,
  AccountNotFound: 3,
  InternalServerError: 4,
  LackResources: 5,
  BannedPlayer: 6
};

export function ok(body) {
  return {
    statusCode: 200,
    body: JSON.stringify({
      retCode: ErrorCode.Success,
      body: JSON.stringify(body),
      error: ""
    })
  };
}

export function fail(retCode, message = "") {
  return {
    statusCode: 200,
    body: JSON.stringify({
      retCode,
      body: "",
      error: message
    })
  };
}
