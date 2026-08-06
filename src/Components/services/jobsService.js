const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "https://localhost:7001/api";

function getAuthToken() {
  return (
    localStorage.getItem("dwumaToken") ||
    sessionStorage.getItem("dwumaToken")
  );
}

export async function getJobs({ signal } = {}) {
  const token = getAuthToken();
  const response = await fetch(`${API_BASE_URL}/jobs`, {
    method: "GET",
    signal,
    headers: {
      Accept: "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  });

  if (!response.ok) {
    throw new Error("Jobs could not be loaded.");
  }

  const payload = await response.json();
  const jobs = Array.isArray(payload)
    ? payload
    : payload.jobs || payload.items || payload.data || [];

  return Array.isArray(jobs) ? jobs : [];
}
