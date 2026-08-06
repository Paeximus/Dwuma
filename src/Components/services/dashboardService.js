const API_BASE_URL = "https://localhost:7001/api";

async function fetchWithAuth(endpoint) {
  const token = localStorage.getItem("dwumaToken");

  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });

  if (!response.ok) {
    throw new Error("Failed to load dashboard data.");
  }

  return response.json();
}

export async function getDashboardData() {
  const [
    user,
    trendingSkills,
    latestInterview,
    recommendedJobs,
  ] = await Promise.all([
    fetchWithAuth("/users/me"),
    fetchWithAuth("/skills/trending"),
    fetchWithAuth("/interviews/latest"),
    fetchWithAuth("/jobs/recommended"),
  ]);

  return {
    user,
    trendingSkills,
    latestInterview,
    recommendedJobs,
  };
}