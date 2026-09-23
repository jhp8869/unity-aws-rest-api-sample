import { ok } from "../shared/apiResponse.mjs";

export const handler = async () => {
  const acceptedVersion = JSON.parse(
    process.env.ACCEPTED_VERSIONS_JSON ?? '[{"Major":4,"Minor":0}]'
  );

  return ok({
    Version: {
      AcceptedVersion: acceptedVersion,
      State: process.env.SERVICE_STATE ?? "Normal",
      Message: process.env.SERVICE_MESSAGE ?? ""
    },
    Time: new Date().toISOString()
  });
};
