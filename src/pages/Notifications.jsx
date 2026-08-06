import { useEffect, useMemo, useState } from "react";
import {
  Bell,
  BriefcaseBusiness,
  CheckCheck,
  Eye,
  MessageSquare,
  Sparkles,
  Trash2,
  TrendingUp,
} from "lucide-react";

import DashboardLayout from "../Components/dashboard/DashboardLayout";
import DashboardPageLoader from "../pages/DashboardPageLoader";
import "./DashboardPages.css";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "https://localhost:7001/api";

const filters = [
  "All",
  "Unread",
  "Jobs",
  "Interview",
  "CV",
  "Skills",
];

function getAuthToken() {
  return (
    localStorage.getItem("dwumaToken") ||
    sessionStorage.getItem("dwumaToken")
  );
}

function getNotificationIcon(category) {
  switch (category?.toLowerCase()) {
    case "jobs":
    case "job":
      return BriefcaseBusiness;

    case "interview":
      return TrendingUp;

    case "cv":
    case "resume":
      return Eye;

    case "skills":
    case "skill":
      return Sparkles;

    default:
      return MessageSquare;
  }
}

function Notifications() {
  const [notifications, setNotifications] = useState([]);
  const [activeFilter, setActiveFilter] = useState("All");
  const [isLoading, setIsLoading] = useState(true);

  async function fetchNotifications() {
    setIsLoading(true);

    try {
      const token = getAuthToken();

      const response = await fetch(
        `${API_BASE_URL}/notifications`,
        {
          method: "GET",
          headers: {
            Accept: "application/json",
            ...(token
              ? {
                  Authorization: `Bearer ${token}`,
                }
              : {}),
          },
        },
      );

      if (!response.ok) {
        return;
      }

      const data = await response.json();

      const loadedNotifications = Array.isArray(data)
        ? data
        : data?.items ||
          data?.notifications ||
          data?.data ||
          [];

      setNotifications(loadedNotifications);
    } catch {
      // Keep the page clean while the backend is unavailable.
      setNotifications([]);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    fetchNotifications();
  }, []);

  const normalizedNotifications = useMemo(
    () =>
      notifications.map((notification, index) => ({
        ...notification,

        id:
          notification.id ||
          notification.notificationId ||
          index,

        title:
          notification.title ||
          notification.subject ||
          "Notification",

        message:
          notification.message ||
          notification.description ||
          notification.content ||
          "",

        category:
          notification.category ||
          notification.type ||
          "General",

        isRead:
          notification.isRead ??
          notification.read ??
          false,

        createdAt:
          notification.createdAt ||
          notification.dateCreated ||
          notification.timestamp ||
          null,
      })),
    [notifications],
  );

  const filteredNotifications = useMemo(() => {
    if (activeFilter === "All") {
      return normalizedNotifications;
    }

    if (activeFilter === "Unread") {
      return normalizedNotifications.filter(
        (notification) => !notification.isRead,
      );
    }

    return normalizedNotifications.filter(
      (notification) =>
        notification.category?.toLowerCase() ===
        activeFilter.toLowerCase(),
    );
  }, [activeFilter, normalizedNotifications]);

  const unreadCount = normalizedNotifications.filter(
    (notification) => !notification.isRead,
  ).length;

  async function markAsRead(notificationId) {
    const selectedNotification =
      normalizedNotifications.find(
        (notification) =>
          notification.id === notificationId,
      );

    if (!selectedNotification || selectedNotification.isRead) {
      return;
    }

    setNotifications((currentNotifications) =>
      currentNotifications.map((notification, index) => {
        const currentId =
          notification.id ||
          notification.notificationId ||
          index;

        return currentId === notificationId
          ? {
              ...notification,
              isRead: true,
              read: true,
            }
          : notification;
      }),
    );

    try {
      const token = getAuthToken();

      await fetch(
        `${API_BASE_URL}/notifications/${notificationId}/read`,
        {
          method: "PATCH",
          headers: {
            ...(token
              ? {
                  Authorization: `Bearer ${token}`,
                }
              : {}),
          },
        },
      );
    } catch {
      // Do not expose backend errors in the interface.
    }
  }

  async function markAllAsRead() {
    setNotifications((currentNotifications) =>
      currentNotifications.map((notification) => ({
        ...notification,
        isRead: true,
        read: true,
      })),
    );

    try {
      const token = getAuthToken();

      await fetch(
        `${API_BASE_URL}/notifications/read-all`,
        {
          method: "PATCH",
          headers: {
            ...(token
              ? {
                  Authorization: `Bearer ${token}`,
                }
              : {}),
          },
        },
      );
    } catch {
      // Do not expose backend errors in the interface.
    }
  }

  async function removeNotification(notificationId) {
    setNotifications((currentNotifications) =>
      currentNotifications.filter((notification, index) => {
        const currentId =
          notification.id ||
          notification.notificationId ||
          index;

        return currentId !== notificationId;
      }),
    );

    try {
      const token = getAuthToken();

      await fetch(
        `${API_BASE_URL}/notifications/${notificationId}`,
        {
          method: "DELETE",
          headers: {
            ...(token
              ? {
                  Authorization: `Bearer ${token}`,
                }
              : {}),
          },
        },
      );
    } catch {
      // Keep the notification interface clean.
    }
  }

  return (
    <DashboardLayout>
      <main className="dashboard-page">
        {isLoading ? (
          <DashboardPageLoader message="Checking your notifications..." />
        ) : (
          <>
            <div className="page-heading page-heading-with-actions">
              <div>
                <p className="page-eyebrow">Updates</p>

                <h1>Notifications</h1>

                <p>
                  {unreadCount > 0
                    ? `${unreadCount} unread ${
                        unreadCount === 1
                          ? "notification"
                          : "notifications"
                      }.`
                    : "Your latest updates will appear here."}
                </p>
              </div>

              <button
                type="button"
                className="secondary-button"
                disabled={unreadCount === 0}
                onClick={markAllAsRead}
              >
                <CheckCheck size={18} />
                Mark all as read
              </button>
            </div>

            <section className="dashboard-card notification-panel">
              <div className="notification-filters">
                {filters.map((filter) => (
                  <button
                    type="button"
                    key={filter}
                    className={
                      activeFilter === filter
                        ? "notification-filter active"
                        : "notification-filter"
                    }
                    onClick={() => setActiveFilter(filter)}
                  >
                    {filter}
                  </button>
                ))}
              </div>

              {filteredNotifications.length === 0 ? (
                <div className="empty-state">
                  <Bell size={30} />

                  <h2>No notifications yet</h2>

                  <p>
                    New job, interview, CV and skill updates will
                    appear here.
                  </p>
                </div>
              ) : (
                <div className="notification-list">
                  {filteredNotifications.map((notification) => {
                    const Icon = getNotificationIcon(
                      notification.category,
                    );

                    return (
                      <article
                        key={notification.id}
                        className={
                          notification.isRead
                            ? "notification-item"
                            : "notification-item unread"
                        }
                        onClick={() =>
                          markAsRead(notification.id)
                        }
                      >
                        <div className="notification-icon">
                          <Icon size={21} />
                        </div>

                        <div className="notification-copy">
                          <div className="notification-title-row">
                            <h2>{notification.title}</h2>

                            {!notification.isRead && (
                              <span
                                className="unread-dot"
                                aria-label="Unread notification"
                              />
                            )}
                          </div>

                          {notification.message && (
                            <p>{notification.message}</p>
                          )}

                          <div className="notification-meta">
                            {notification.category && (
                              <span>
                                {notification.category}
                              </span>
                            )}

                            {notification.createdAt && (
                              <time>
                                {new Date(
                                  notification.createdAt,
                                ).toLocaleString()}
                              </time>
                            )}
                          </div>
                        </div>

                        <button
                          type="button"
                          className="notification-delete"
                          aria-label="Delete notification"
                          onClick={(event) => {
                            event.stopPropagation();
                            removeNotification(notification.id);
                          }}
                        >
                          <Trash2 size={18} />
                        </button>
                      </article>
                    );
                  })}
                </div>
              )}
            </section>
          </>
        )}
      </main>
    </DashboardLayout>
  );
}

export default Notifications;