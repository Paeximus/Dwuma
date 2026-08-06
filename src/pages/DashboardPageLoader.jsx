// src/Components/dashboard/DashboardPageLoader.jsx

function DashboardPageLoader({ message = "Preparing your account..." }) {
  return (
    <div className="dashboard-page-loader">
      <div className="dashboard-loader-spinner" />

      <h2>Just a moment</h2>
      <p>{message}</p>

      <div className="dashboard-loader-dots">
        <span className="active" />
        <span />
        <span />
      </div>
    </div>
  );
}

export default DashboardPageLoader;