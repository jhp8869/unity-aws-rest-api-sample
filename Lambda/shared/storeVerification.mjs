import axios from "axios";
import { google } from "googleapis";

const oneStoreHosts = {
  sandbox: "https://sbpp.onestore.net",
  production: "https://iap-apis.onestore.net"
};

export async function verifyGooglePurchase({ packageName, productId, purchaseToken }) {
  const credentials = JSON.parse(process.env.GOOGLE_SERVICE_ACCOUNT_KEY);
  const auth = new google.auth.GoogleAuth({
    credentials,
    scopes: ["https://www.googleapis.com/auth/androidpublisher"]
  });

  const androidPublisher = google.androidpublisher({
    version: "v3",
    auth
  });

  const response = await androidPublisher.purchases.products.get({
    packageName,
    productId,
    token: purchaseToken
  });

  if (response.data.purchaseState !== 0) {
    throw new Error("Google purchase is not completed");
  }

  return response.data;
}

export async function verifyOneStorePurchase({ productId, purchaseToken }) {
  const accessToken = await getOneStoreAccessToken();
  const baseUrl = getOneStoreBaseUrl();
  const packageName = process.env.ONESTORE_PACKAGE_NAME;

  const response = await axios.post(
    `${baseUrl}/v7/apps/${packageName}/purchases/inapp/products/${productId}/${purchaseToken}/consume`,
    {},
    {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        "Content-Type": "application/json",
        "x-market-code": "MKT_ONE"
      }
    }
  );

  if (response.data?.result?.code !== "Success") {
    throw new Error("One Store purchase consume failed");
  }

  return response.data;
}

async function getOneStoreAccessToken() {
  const baseUrl = getOneStoreBaseUrl();
  const data = new URLSearchParams();
  data.append("grant_type", "client_credentials");
  data.append("client_id", process.env.ONESTORE_CLIENT_ID);
  data.append("client_secret", process.env.ONESTORE_CLIENT_SECRET);

  const response = await axios.post(`${baseUrl}/v7/oauth/token`, data, {
    headers: {
      "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8",
      "x-market-code": "MKT_ONE"
    }
  });

  return response.data.access_token;
}

function getOneStoreBaseUrl() {
  return process.env.STAGE === "dev"
    ? oneStoreHosts.sandbox
    : oneStoreHosts.production;
}
