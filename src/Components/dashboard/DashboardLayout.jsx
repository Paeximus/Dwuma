import { useState } from "react";

import DashboardHeader from "./DashboardHeader";
import DashboardSidebar from "./DashboardSidebar";

function DashboardLayout({
  children,
  user,
  unreadNotifications = 0,
}) {
  const [isSidebarOpen, setIsSidebarOpen] =
    useState(false);

  return (
    <div className="dashboard-shell">
      <DashboardSidebar
        isOpen={isSidebarOpen}
        unreadNotifications={unreadNotifications}
        onClose={() => setIsSidebarOpen(false)}
      />

      {isSidebarOpen && (
        <button
          type="button"
          className="dashboard-sidebar-overlay"
          aria-label="Close sidebar"
          onClick={() => setIsSidebarOpen(false)}
        />
      )}

      <div className="dashboard-main">
        <DashboardHeader
          user={user}
          unreadNotifications={unreadNotifications}
          onOpenSidebar={() =>
            setIsSidebarOpen(true)
          }
        />

        <main className="dashboard-content">
          {children}
        </main>

        <footer className="dashboard-footer">
          © {new Date().getFullYear()} DWUMA. All
          rights reserved.
        </footer>
      </div>
    </div>
  );
}

export default DashboardLayout;