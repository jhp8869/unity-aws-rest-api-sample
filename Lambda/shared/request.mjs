export function parseBody(event) {
  if (!event?.body) return {};
  return typeof event.body === "string" ? JSON.parse(event.body) : event.body;
}

/** Cognito authorizer가 있으면 클라이언트 PlayerId보다 토큰의 subject를 우선한다. */
export function getAuthorizedPlayerId(event, body) {
  return event?.requestContext?.authorizer?.claims?.["cognito:username"]
    ?? event?.requestContext?.authorizer?.jwt?.claims?.sub
    ?? body?.PlayerId;
}
