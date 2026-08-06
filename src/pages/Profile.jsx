import { useEffect, useState } from "react";
import {
  BriefcaseBusiness,
  FileText,
  GraduationCap,
  Mail,
  MapPin,
  Pencil,
  Phone,
  Upload,
  UserRound,
} from "lucide-react";

import DashboardLayout from "../Components/dashboard/DashboardLayout";
import DashboardPageLoader from "../pages/DashboardPageLoader";
import "./DashboardPages.css";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL || "https://localhost:7001/api";

function getAuthToken() {
  return (
    localStorage.getItem("dwumaToken") ||
    sessionStorage.getItem("dwumaToken")
  );
}

function Profile() {
  const [profile, setProfile] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  async function fetchProfile() {
    setIsLoading(true);

    try {
      const token = getAuthToken();

      const response = await fetch(`${API_BASE_URL}/profile`, {
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
      setProfile(data);
    } catch {
      // Keep a clean empty state while the backend is unavailable.
      setProfile(null);
    } finally {
      setIsLoading(false);
    }
  }

  useEffect(() => {
    fetchProfile();
  }, []);

  const information = profile
    ? [
        {
          label: "Full Name",
          value:
            profile.fullName ||
            profile.name ||
            profile.username,
          icon: UserRound,
        },
        {
          label: "Email Address",
          value: profile.email,
          icon: Mail,
        },
        {
          label: "Phone Number",
          value:
            profile.phoneNumber ||
            profile.phone,
          icon: Phone,
        },
        {
          label: "Location",
          value: profile.location,
          icon: MapPin,
        },
        {
          label: "Career Field",
          value:
            profile.careerField ||
            profile.fieldOfStudy ||
            profile.preferredRole,
          icon: BriefcaseBusiness,
        },
        {
          label: "Education",
          value:
            profile.education ||
            profile.educationLevel ||
            profile.degree,
          icon: GraduationCap,
        },
      ].filter((item) => item.value)
    : [];

  const skills = Array.isArray(profile?.skills)
    ? profile.skills
    : [];

  const resume =
    profile?.resume ||
    profile?.cv ||
    profile?.uploadedResume ||
    null;

  return (
    <DashboardLayout>
      <main className="dashboard-page">
        {isLoading ? (
          <DashboardPageLoader message="Loading your profile information..." />
        ) : (
          <>
            <div className="page-heading">
              <div>
                <p className="page-eyebrow">Account</p>

                <h1>Profile</h1>

                <p>
                  Manage your professional information.
                </p>
              </div>
            </div>

            <section className="profile-grid">
              <article className="dashboard-card profile-information-card">
                <div className="card-heading">
                  <div>
                    <h2>Personal Information</h2>

                    <p>
                      Your account and career details.
                    </p>
                  </div>

                  <button
                    type="button"
                    className="secondary-button"
                  >
                    <Pencil size={17} />
                    Edit Profile
                  </button>
                </div>

                {information.length > 0 ? (
                  <div className="information-grid">
                    {information.map((item) => {
                      const Icon = item.icon;

                      return (
                        <div
                          className="information-item"
                          key={item.label}
                        >
                          <div className="information-icon">
                            <Icon size={19} />
                          </div>

                          <div>
                            <span>{item.label}</span>
                            <strong>{item.value}</strong>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                ) : (
                  <div className="inline-empty-state">
                    <UserRound size={25} />

                    <p>
                      Your account information will appear here
                      after your profile is completed.
                    </p>
                  </div>
                )}
              </article>

              <article className="dashboard-card skills-card">
                <div className="card-heading">
                  <div>
                    <h2>Skills</h2>

                    <p>
                      Skills visible to recruiters.
                    </p>
                  </div>

                  <button
                    type="button"
                    className="secondary-button"
                  >
                    <Pencil size={17} />
                    Edit Skills
                  </button>
                </div>

                {skills.length > 0 ? (
                  <div className="skills-list">
                    {skills.map((skill, index) => {
                      const skillName =
                        typeof skill === "string"
                          ? skill
                          : skill.name ||
                            skill.skillName ||
                            skill.title;

                      if (!skillName) {
                        return null;
                      }

                      return (
                        <span
                          className="skill-pill"
                          key={
                            skill.id ||
                            skill.skillId ||
                            `${skillName}-${index}`
                          }
                        >
                          {skillName}
                        </span>
                      );
                    })}
                  </div>
                ) : (
                  <div className="inline-empty-state">
                    <p>
                      Your skills will appear here after they are
                      added.
                    </p>
                  </div>
                )}
              </article>

              <article className="dashboard-card resume-card">
                <div className="card-heading">
                  <div>
                    <h2>Resume</h2>

                    <p>
                      Manage the CV used for applications.
                    </p>
                  </div>
                </div>

                {resume ? (
                  <div className="resume-content">
                    <div className="resume-file">
                      <div className="resume-icon">
                        <FileText size={24} />
                      </div>

                      <div>
                        <strong>
                          {resume.fileName ||
                            resume.name ||
                            "Uploaded resume"}
                        </strong>

                        <span>
                          {resume.uploadedAt
                            ? `Uploaded ${new Date(
                                resume.uploadedAt,
                              ).toLocaleDateString()}`
                            : "Resume uploaded"}
                        </span>
                      </div>
                    </div>

                    <div className="resume-actions">
                      <button
                        type="button"
                        className="primary-button"
                      >
                        <Upload size={17} />
                        Replace Resume
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className="inline-empty-state resume-empty-state">
                    <FileText size={25} />

                    <p>
                      No resume has been uploaded yet.
                    </p>

                    <button
                      type="button"
                      className="primary-button"
                    >
                      <Upload size={17} />
                      Upload Resume
                    </button>
                  </div>
                )}
              </article>
            </section>
          </>
        )}
      </main>
    </DashboardLayout>
  );
}

export default Profile;