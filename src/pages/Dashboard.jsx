import DashboardLayout from "../Components/dashboard/DashboardLayout";
import InterviewScoreCard from "../Components/dashboard/InterviewScoreCard";
import RecommendedJobs from "../Components/dashboard/RecommendedJobs";
import SkillGapCard from "../Components/dashboard/SkillGapCard";

import "../Components/dashboard/Dashboard.css";

function Dashboard() {
  /*
    Replace these null and empty values with backend data later.

    Example endpoint:
    GET /api/dashboard
  */

  const dashboardData = null;

  const user = dashboardData?.user ?? null;
  const skills = dashboardData?.trendingSkills ?? [];
  const interview = dashboardData?.latestInterview ?? null;
  const jobs = dashboardData?.recommendedJobs ?? [];
  const unreadNotifications =
    dashboardData?.unreadNotifications ?? 0;

  return (
    <DashboardLayout
      user={user}
      unreadNotifications={unreadNotifications}
    >
      <section className="dashboard-overview-grid">
        <SkillGapCard skills={skills} />

        <InterviewScoreCard interview={interview} />
      </section>

      <RecommendedJobs jobs={jobs} />
    </DashboardLayout>
  );
}

export default Dashboard;