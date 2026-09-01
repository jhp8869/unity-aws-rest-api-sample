export function createLogger(playerId = null) {
  const write = (level, message, data = {}) => {
    console.log(JSON.stringify({
      level,
      message,
      timestamp: new Date().toISOString(),
      playerId,
      ...data
    }, (_, value) => typeof value === "bigint" ? value.toString() : value));
  };

  return {
    info: (message, data = {}) => write("info", message, data),
    warn: (message, data = {}) => write("warn", message, data),
    error: (message, error, data = {}) => write("error", message, {
      error: error?.message ?? String(error),
      ...data
    })
  };
}
