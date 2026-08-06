import {
  Bell,
  BriefcaseBusiness,
  FileText,
  LayoutDashboard,
  LogOut,
  Settings,
  UserRound,
  UsersRound,
} from "lucide-react";

import { useLocation } from "wouter";

const navigationItems = [
  {
    label: "Dashboard",
    path: "/dashboard",
    icon: LayoutDashboard,
  },
  {
    label: "Interview Coach",
    path: "/dashboard/interview-coach",
    icon: UsersRound,
  },
  {
    label: "CV Tailor",
    path: "/dashboard/cv-tailor",
    icon: FileText,
  },
  {
    label: "Jobs",
    path: "/dashboard/jobs",
    icon: BriefcaseBusiness,
  },
  {
    label: "Profile",
    path: "/dashboard/profile",
    icon: UserRound,
  },
  {
    label: "Notifications",
    path: "/dashboard/notifications",
    icon: Bell,
  },
  {
    label: "Settings",
    path: "/dashboard/settings",
    icon: Settings,
  },
];

function DashboardSidebar({
  unreadNotifications = 0,
  isOpen = false,
  onClose,
}) {
  const [location, navigate] = useLocation();

  function isActive(path) {
    if (path === "/dashboard") {
      return location === "/dashboard";
    }

    return location.startsWith(path);
  }

  function handleNavigation(path) {
    navigate(path);
    onClose?.();
  }

  function handleLogout() {
    localStorage.removeItem("dwumaLogin");
    sessionStorage.removeItem("dwumaLogin");
    navigate("/login");
  }

  return (
    <aside
      className={`dashboard-sidebar ${
        isOpen ? "dashboard-sidebar-open" : ""
      }`}
    >
      <div className="dashboard-sidebar-brand">
        DWUMA
      </div>

      <nav className="dashboard-sidebar-navigation">
        {navigationItems.map((item) => {
          const Icon = item.icon;
          const active = isActive(item.path);

          return (
            <button
              key={item.path}
              type="button"
              className={`dashboard-sidebar-link ${
                active
                  ? "dashboard-sidebar-link-active"
                  : ""
              }`}
              onClick={() =>
                handleNavigation(item.path)
              }
            >
              <Icon
                size={18}
                strokeWidth={2}
                aria-hidden="true"
              />

              <span>{item.label}</span>

              {item.label === "Notifications" &&
                unreadNotifications > 0 && (
                  <span className="dashboard-sidebar-badge">
                    {unreadNotifications > 9
                      ? "9+"
                      : unreadNotifications}
                  </span>
                )}
            </button>
          );
        })}
      </nav>

      <div className="dashboard-sidebar-profile">
        <div className="dashboard-sidebar-avatar">
          {/*
            Profile image or initials can come
            from the backend later.
          */}
          <UserRound size={19} />
        </div>

        <div className="dashboard-sidebar-profile-text">
          <strong>Account</strong>
          <span>Graduate</span>
        </div>
      </div>

      <button
        type="button"
        className="dashboard-sidebar-logout"
        onClick={handleLogout}
      >
        <LogOut size={18} />
        <span>Log out</span>
      </button>
    </aside>
  );
}

export default DashboardSidebar;