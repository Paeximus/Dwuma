import { useEffect, useState } from "react";
import {
  Bell,
  Eye,
  LockKeyhole,
  Pencil,
  Save,
  Trash2,
  UserRound,
} from "lucide-react";

import DashboardLayout from "../Components/dashboard/DashboardLayout";
import DashboardPageLoader from "../pages/DashboardPageLoader";
import "./DashboardPages.css";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "https://localhost:7001/api";

const settingsSections = [
  {
    id: "account",
    label: "Account",
    icon: UserRound,
  },
  {
    id: "security",
    label: "Security",
    icon: LockKeyhole,
  },
  {
    id: "notifications",
    label: "Notifications",
    icon: Bell,
  },
  {
    id: "privacy",
    label: "Privacy",
    icon: Eye,
  },
];

const initialSettings = {
  account: {
    fullName: "",
    email: "",
    phoneNumber: "",
    location: "",
    careerField: "",
  },

  notifications: {
    emailNotifications: false,
    jobRecommendations: false,
    interviewReminders: false,
    cvUpdates: false,
    marketingEmails: false,
  },

  privacy: {
    publicProfile: false,
    recruiterVisibility: false,
    activityVisibility: false,
  },
};

function getAuthToken() {
  return (
    localStorage.getItem("dwumaToken") ||
    sessionStorage.getItem("dwumaToken")
  );
}

function ToggleSwitch({
  checked,
  onChange,
  label,
  disabled = false,
}) {
  return (
    <button
      type="button"
      className={checked ? "toggle-switch active" : "toggle-switch"}
      role="switch"
      aria-checked={checked}
      aria-label={label}
      disabled={disabled}
      onClick={() => onChange(!checked)}
    >
      <span />
    </button>
  );
}

function Settings() {
  const [activeSection, setActiveSection] = useState("account");

  const [settings, setSettings] = useState(initialSettings);
  const [savedAccount, setSavedAccount] = useState(
    initialSettings.account,
  );

  const [isLoading, setIsLoading] = useState(true);
  const [isEditingAccount, setIsEditingAccount] = useState(false);
  const [isSavingAccount, setIsSavingAccount] = useState(false);
  const [savingSection, setSavingSection] = useState("");

  async function fetchSettings() {
    setIsLoading(true);

    try {
      const token = getAuthToken();

      const response = await fetch(`${API_BASE_URL}/settings`, {
        method: "GET",
        headers: {
          Accept: "application/json",
          ...(token
            ? {
                Authorization: `Bearer ${token}`,
              }
            : {}),
        },
      });

      if (!response.ok) {
        return;
      }

      const data = await response.json();

      const loadedSettings = {
        account: {
          fullName: data?.account?.fullName || "",
          email: data?.account?.email || "",
          phoneNumber: data?.account?.phoneNumber || "",
          location: data?.account?.location || "",
          careerField: data?.account?.careerField || "",
        },

        notifications: {
          emailNotifications:
            data?.notifications?.emailNotifications ?? false,

          jobRecommendations:
            data?.notifications?.jobRecommendations ?? false,

          interviewReminders:
            data?.notifications?.interviewReminders ?? false,

          cvUpdates:
            data?.notifications?.cvUpdates ?? false,

          marketingEmails:
            data?.notifications?.marketingEmails ?? false,
        },

        privacy: {
          publicProfile:
            data?.privacy?.publicProfile ?? false,

          recruiterVisibility:
            data?.privacy?.recruiterVisibility ?? false,

          activityVisibility:
            data?.privacy?.activityVisibility ?? false,
        },
      };

      setSettings(loadedSettings);
      setSavedAccount(loadedSettings.account);
    } catch {
      // Keep empty settings while the backend is unavailable.
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    fetchSettings();
  }, []);

  function handleAccountChange(event) {
    const { name, value } = event.target;

    setSettings((currentSettings) => ({
      ...currentSettings,

      account: {
        ...currentSettings.account,
        [name]: value,
      },
    }));
  }

  async function saveAccountInformation() {
    setIsSavingAccount(true);

    try {
      const token = getAuthToken();

      const response = await fetch(
        `${API_BASE_URL}/settings/account`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
            ...(token
              ? {
                  Authorization: `Bearer ${token}`,
                }
              : {}),
          },
          body: JSON.stringify(settings.account),
        },
      );

      if (!response.ok) {
        return;
      }

      setSavedAccount(settings.account);
      setIsEditingAccount(false);
    } catch {
      // Keep the form open if saving fails.
    } finally {
      setIsSavingAccount(false);
    }
  }

  function cancelAccountEditing() {
    setSettings((currentSettings) => ({
      ...currentSettings,
      account: savedAccount,
    }));

    setIsEditingAccount(false);
  }

  async function updateSettingsSection(
    sectionName,
    updatedValues,
  ) {
    const previousValues = settings[sectionName];

    setSettings((currentSettings) => ({
      ...currentSettings,
      [sectionName]: updatedValues,
    }));

    setSavingSection(sectionName);

    try {
      const token = getAuthToken();

      const response = await fetch(
        `${API_BASE_URL}/settings/${sectionName}`,
        {
          method: "PUT",
          headers: {
            "Content-Type": "application/json",
            ...(token
              ? {
                  Authorization: `Bearer ${token}`,
                }
              : {}),
          },
          body: JSON.stringify(updatedValues),
        },
      );

      if (!response.ok) {
        setSettings((currentSettings) => ({
          ...currentSettings,
          [sectionName]: previousValues,
        }));
      }
    } catch {
      setSettings((currentSettings) => ({
        ...currentSettings,
        [sectionName]: previousValues,
      }));
    } finally {
      setSavingSection("");
    }
  }

  function handleNotificationChange(settingName, value) {
    updateSettingsSection("notifications", {
      ...settings.notifications,
      [settingName]: value,
    });
  }

  function handlePrivacyChange(settingName, value) {
    updateSettingsSection("privacy", {
      ...settings.privacy,
      [settingName]: value,
    });
  }

  async function deleteAccount() {
    const confirmed = window.confirm(
      "Are you sure you want to permanently delete your account?",
    );

    if (!confirmed) {
      return;
    }

    try {
      const token = getAuthToken();

      const response = await fetch(`${API_BASE_URL}/account`, {
        method: "DELETE",
        headers: {
          ...(token
            ? {
                Authorization: `Bearer ${token}`,
              }
            : {}),
        },
      });

      if (!response.ok) {
        return;
      }

      localStorage.removeItem("dwumaToken");
      sessionStorage.removeItem("dwumaToken");

      localStorage.removeItem("dwumaLogin");
      sessionStorage.removeItem("dwumaLogin");

      window.location.href = "/login";
    } catch {
      // Keep technical errors out of the interface.
    }
  }

  return (
    <DashboardLayout>
      <main className="dashboard-page">
        {isLoading ? (
          <DashboardPageLoader message="Preparing your settings..." />
        ) : (
          <>
            <div className="page-heading">
              <div>
                <p className="page-eyebrow">Preferences</p>

                <h1>Settings</h1>

                <p>
                  Manage your account and application preferences.
                </p>
              </div>
            </div>

            <section className="settings-layout">
              <aside className="dashboard-card settings-navigation">
                {settingsSections.map((section) => {
                  const Icon = section.icon;

                  return (
                    <button
                      type="button"
                      key={section.id}
                      className={
                        activeSection === section.id
                          ? "settings-navigation-item active"
                          : "settings-navigation-item"
                      }
                      onClick={() =>
                        setActiveSection(section.id)
                      }
                    >
                      <Icon size={19} />
                      <span>{section.label}</span>
                    </button>
                  );
                })}
              </aside>

              <div className="settings-content">
                {activeSection === "account" && (
                  <>
                    <article className="dashboard-card settings-section">
                      <div className="card-heading">
                        <div>
                          <h2>Account Information</h2>

                          <p>
                            Update your personal and contact
                            information.
                          </p>
                        </div>

                        {!isEditingAccount && (
                          <button
                            type="button"
                            className="secondary-button"
                            onClick={() =>
                              setIsEditingAccount(true)
                            }
                          >
                            <Pencil size={17} />
                            Edit
                          </button>
                        )}
                      </div>

                      <div className="settings-form">
                        <label>
                          <span>Full Name</span>

                          <input
                            type="text"
                            name="fullName"
                            value={settings.account.fullName}
                            disabled={!isEditingAccount}
                            onChange={handleAccountChange}
                            placeholder="Not provided"
                          />
                        </label>

                        <label>
                          <span>Email Address</span>

                          <input
                            type="email"
                            name="email"
                            value={settings.account.email}
                            disabled={!isEditingAccount}
                            onChange={handleAccountChange}
                            placeholder="Not provided"
                          />
                        </label>

                        <label>
                          <span>Phone Number</span>

                          <input
                            type="tel"
                            name="phoneNumber"
                            value={settings.account.phoneNumber}
                            disabled={!isEditingAccount}
                            onChange={handleAccountChange}
                            placeholder="Not provided"
                          />
                        </label>

                        <label>
                          <span>Location</span>

                          <input
                            type="text"
                            name="location"
                            value={settings.account.location}
                            disabled={!isEditingAccount}
                            onChange={handleAccountChange}
                            placeholder="Not provided"
                          />
                        </label>

                        <label className="form-field-full">
                          <span>Career Field</span>

                          <input
                            type="text"
                            name="careerField"
                            value={settings.account.careerField}
                            disabled={!isEditingAccount}
                            onChange={handleAccountChange}
                            placeholder="Not provided"
                          />
                        </label>
                      </div>

                      {isEditingAccount && (
                        <div className="form-actions">
                          <button
                            type="button"
                            className="secondary-button"
                            disabled={isSavingAccount}
                            onClick={cancelAccountEditing}
                          >
                            Cancel
                          </button>

                          <button
                            type="button"
                            className="primary-button"
                            disabled={isSavingAccount}
                            onClick={saveAccountInformation}
                          >
                            <Save size={17} />

                            {isSavingAccount
                              ? "Saving..."
                              : "Save Changes"}
                          </button>
                        </div>
                      )}
                    </article>

                    <article className="dashboard-card danger-zone">
                      <div>
                        <h2>Delete Account</h2>

                        <p>
                          Permanently delete your DWUMA account and
                          all associated information.
                        </p>
                      </div>

                      <button
                        type="button"
                        className="danger-button"
                        onClick={deleteAccount}
                      >
                        <Trash2 size={17} />
                        Delete Account
                      </button>
                    </article>
                  </>
                )}

                {activeSection === "security" && (
                  <article className="dashboard-card settings-section">
                    <div className="card-heading">
                      <div>
                        <h2>Security</h2>

                        <p>
                          Manage your password and account security.
                        </p>
                      </div>
                    </div>

                    <div className="settings-action-list">
                      <div className="settings-action-item">
                        <div className="settings-action-icon">
                          <LockKeyhole size={20} />
                        </div>

                        <div>
                          <strong>Change Password</strong>

                          <span>
                            Update the password used to access your
                            account.
                          </span>
                        </div>

                        <button
                          type="button"
                          className="secondary-button"
                        >
                          Change
                        </button>
                      </div>
                    </div>
                  </article>
                )}

                {activeSection === "notifications" && (
                  <article className="dashboard-card settings-section">
                    <div className="card-heading">
                      <div>
                        <h2>Notification Preferences</h2>

                        <p>
                          Choose the updates you want to receive.
                        </p>
                      </div>
                    </div>

                    <div className="preference-switch-list">
                      <div className="preference-switch-item">
                        <div>
                          <strong>Email Notifications</strong>

                          <span>
                            Receive important account updates through
                            email.
                          </span>
                        </div>

                        <ToggleSwitch
                          label="Email Notifications"
                          checked={
                            settings.notifications
                              .emailNotifications
                          }
                          disabled={
                            savingSection === "notifications"
                          }
                          onChange={(value) =>
                            handleNotificationChange(
                              "emailNotifications",
                              value,
                            )
                          }
                        />
                      </div>

                      <div className="preference-switch-item">
                        <div>
                          <strong>Job Recommendations</strong>

                          <span>
                            Receive alerts for jobs matching your
                            profile.
                          </span>
                        </div>

                        <ToggleSwitch
                          label="Job Recommendations"
                          checked={
                            settings.notifications
                              .jobRecommendations
                          }
                          disabled={
                            savingSection === "notifications"
                          }
                          onChange={(value) =>
                            handleNotificationChange(
                              "jobRecommendations",
                              value,
                            )
                          }
                        />
                      </div>

                      <div className="preference-switch-item">
                        <div>
                          <strong>Interview Reminders</strong>

                          <span>
                            Receive reminders about interview
                            practice.
                          </span>
                        </div>

                        <ToggleSwitch
                          label="Interview Reminders"
                          checked={
                            settings.notifications
                              .interviewReminders
                          }
                          disabled={
                            savingSection === "notifications"
                          }
                          onChange={(value) =>
                            handleNotificationChange(
                              "interviewReminders",
                              value,
                            )
                          }
                        />
                      </div>

                      <div className="preference-switch-item">
                        <div>
                          <strong>CV Updates</strong>

                          <span>
                            Receive updates related to your CV.
                          </span>
                        </div>

                        <ToggleSwitch
                          label="CV Updates"
                          checked={
                            settings.notifications.cvUpdates
                          }
                          disabled={
                            savingSection === "notifications"
                          }
                          onChange={(value) =>
                            handleNotificationChange(
                              "cvUpdates",
                              value,
                            )
                          }
                        />
                      </div>

                      <div className="preference-switch-item">
                        <div>
                          <strong>Marketing Emails</strong>

                          <span>
                            Receive promotional messages and
                            announcements.
                          </span>
                        </div>

                        <ToggleSwitch
                          label="Marketing Emails"
                          checked={
                            settings.notifications.marketingEmails
                          }
                          disabled={
                            savingSection === "notifications"
                          }
                          onChange={(value) =>
                            handleNotificationChange(
                              "marketingEmails",
                              value,
                            )
                          }
                        />
                      </div>
                    </div>
                  </article>
                )}

                {activeSection === "privacy" && (
                  <article className="dashboard-card settings-section">
                    <div className="card-heading">
                      <div>
                        <h2>Privacy</h2>

                        <p>
                          Control who can see your profile and
                          activity.
                        </p>
                      </div>
                    </div>

                    <div className="preference-switch-list">
                      <div className="preference-switch-item">
                        <div>
                          <strong>Public Profile</strong>

                          <span>
                            Allow your profile to appear in search
                            results.
                          </span>
                        </div>

                        <ToggleSwitch
                          label="Public Profile"
                          checked={
                            settings.privacy.publicProfile
                          }
                          disabled={savingSection === "privacy"}
                          onChange={(value) =>
                            handlePrivacyChange(
                              "publicProfile",
                              value,
                            )
                          }
                        />
                      </div>

                      <div className="preference-switch-item">
                        <div>
                          <strong>Recruiter Visibility</strong>

                          <span>
                            Allow verified recruiters to view your
                            profile.
                          </span>
                        </div>

                        <ToggleSwitch
                          label="Recruiter Visibility"
                          checked={
                            settings.privacy.recruiterVisibility
                          }
                          disabled={savingSection === "privacy"}
                          onChange={(value) =>
                            handlePrivacyChange(
                              "recruiterVisibility",
                              value,
                            )
                          }
                        />
                      </div>

                      <div className="preference-switch-item">
                        <div>
                          <strong>Activity Visibility</strong>

                          <span>
                            Show your recent career and learning
                            activity.
                          </span>
                        </div>

                        <ToggleSwitch
                          label="Activity Visibility"
                          checked={
                            settings.privacy.activityVisibility
                          }
                          disabled={savingSection === "privacy"}
                          onChange={(value) =>
                            handlePrivacyChange(
                              "activityVisibility",
                              value,
                            )
                          }
                        />
                      </div>
                    </div>
                  </article>
                )}
              </div>
            </section>
          </>
        )}
      </main>
    </DashboardLayout>
  );
}

export default Settings;