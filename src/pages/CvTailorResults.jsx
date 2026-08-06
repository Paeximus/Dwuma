import { useEffect, useState } from "react";
import { useLocation } from "wouter";
import {
  ArrowLeft,
  Bell,
  BriefcaseBusiness,
  Check,
  Download,
  FileText,
  LayoutDashboard,
  LoaderCircle,
  Menu,
  RefreshCcw,
  Settings,
  UserRound,
  UsersRound,
} from "lucide-react";

import logo from "../assets/logo.svg";
import "./CvTailorResults.css";

function CvTailorResults() {
  const [, navigate] = useLocation();

  const [menuOpen, setMenuOpen] = useState(false);
  const [tailorRequest, setTailorRequest] = useState(null);
  const [tailorResult, setTailorResult] = useState(null);
  const [isLoadingStorage, setIsLoadingStorage] =
    useState(true);
  const [statusMessage, setStatusMessage] = useState("");

  useEffect(() => {
    const savedRequest = sessionStorage.getItem(
      "dwumaCvTailorRequest"
    );

    const savedResult = sessionStorage.getItem(
      "dwumaCvTailorResult"
    );

    if (savedRequest) {
      try {
        setTailorRequest(JSON.parse(savedRequest));
      } catch (error) {
        console.error(
          "Unable to read the CV tailor request:",
          error
        );

        sessionStorage.removeItem(
          "dwumaCvTailorRequest"
        );
      }
    }

    if (savedResult) {
      try {
        setTailorResult(JSON.parse(savedResult));
      } catch (error) {
        console.error(
          "Unable to read the CV tailor result:",
          error
        );

        sessionStorage.removeItem(
          "dwumaCvTailorResult"
        );
      }
    }

    setIsLoadingStorage(false);
  }, []);

  function closeMenu() {
    setMenuOpen(false);
  }

  function goToPage(path) {
    closeMenu();
    navigate(path);
  }

  function handleTailorAnotherCv() {
    sessionStorage.removeItem(
      "dwumaCvTailorRequest"
    );

    sessionStorage.removeItem(
      "dwumaCvTailorResult"
    );

    setTailorRequest(null);
    setTailorResult(null);
    setStatusMessage("");

    navigate("/dashboard/cv-tailor");
  }

  function handleDownload(format) {
    if (!tailorResult) {
      setStatusMessage(
        "The tailored CV is not available yet."
      );
      return;
    }

    const downloadUrl =
      format === "pdf"
        ? tailorResult.pdfUrl
        : tailorResult.docxUrl;

    if (!downloadUrl) {
      setStatusMessage(
        `${format.toUpperCase()} download is not available yet.`
      );
      return;
    }

    window.open(
      downloadUrl,
      "_blank",
      "noopener,noreferrer"
    );
  }

  function renderPageContent() {
    if (isLoadingStorage) {
      return (
        <section className="cv-results-state-card">
          <LoaderCircle
            className="cv-results-spinner"
            size={42}
          />

          <h1>Loading CV results</h1>

          <p>
            Please wait while your CV information is
            being prepared.
          </p>
        </section>
      );
    }

    if (!tailorRequest) {
      return (
        <section className="cv-results-state-card">
          <div className="cv-results-state-icon">
            <FileText size={34} />
          </div>

          <h1>No tailored CV available</h1>

          <p>
            Upload your CV before opening the results
            page.
          </p>

          <button
            type="button"
            className="cv-results-state-button"
            onClick={() =>
              navigate("/dashboard/cv-tailor")
            }
          >
            Upload a CV
          </button>
        </section>
      );
    }

    if (!tailorResult) {
      return (
        <section className="cv-results-state-card">
          <LoaderCircle
            className="cv-results-spinner"
            size={45}
          />

          <h1>Your CV is being tailored</h1>

          <p className="cv-results-file-name">
            Uploaded file:{" "}
            <strong>{tailorRequest.fileName}</strong>
          </p>

          <p>
            The tailored CV will appear here after the
            backend processes the uploaded document.
          </p>

          <button
            type="button"
            className="cv-results-state-button"
            onClick={() =>
              navigate("/dashboard/cv-tailor")
            }
          >
            Back
          </button>
        </section>
      );
    }

    const improvements = Array.isArray(
      tailorResult.improvements
    )
      ? tailorResult.improvements
      : [];

    return (
      <>
        <section className="cv-results-introduction">
          <div className="cv-results-success-icon">
            <Check size={24} strokeWidth={3} />
          </div>

          <div>
            <h1>Your CV is Ready!</h1>

            <p>
              Your uploaded CV has been successfully
              tailored.
            </p>
          </div>
        </section>

        <section className="cv-results-statistics">
          <article className="cv-stat-card">
            <p>ATS Score</p>

            <strong>
              {tailorResult.atsScore ?? "--"}
              {tailorResult.atsScore != null ? "%" : ""}
            </strong>

            <span>ATS compatibility</span>
          </article>

          <article className="cv-stat-card">
            <p>Job Match</p>

            <strong>
              {tailorResult.matchScore ?? "--"}
              {tailorResult.matchScore != null ? "%" : ""}
            </strong>

            <span>Role relevance</span>
          </article>

          <article className="cv-stat-card">
            <p>Changes Made</p>

            <strong>
              {tailorResult.changesMade ?? "--"}
            </strong>

            <span>CV improvements</span>
          </article>
        </section>

        <section className="cv-results-main-grid">
          <article className="cv-preview-card">
            <div className="cv-preview-heading">
              <div>
                <h2>Tailored CV</h2>

                <p>{tailorRequest.fileName}</p>
              </div>

              <span className="cv-preview-badge">
                Corrected
              </span>
            </div>

            <div className="cv-preview-window">
              {tailorResult.previewHtml ? (
                <div
                  className="cv-document"
                  dangerouslySetInnerHTML={{
                    __html: tailorResult.previewHtml,
                  }}
                />
              ) : tailorResult.previewText ? (
                <div className="cv-document cv-document-text">
                  {tailorResult.previewText}
                </div>
              ) : (
                <div className="cv-preview-placeholder">
                  <FileText size={40} />

                  <h3>Preview unavailable</h3>

                  <p>
                    The backend did not return preview
                    content for the tailored CV.
                  </p>
                </div>
              )}
            </div>
          </article>

          <aside className="cv-results-sidebar">
            <article className="cv-improvements-card">
              <div className="cv-improvements-heading">
                <span>✦</span>

                <h2>AI Improvements</h2>
              </div>

              {improvements.length > 0 ? (
                <div className="cv-improvements-list">
                  {improvements.map(
                    (improvement, index) => (
                      <div
                        key={`${improvement}-${index}`}
                        className="cv-improvement-item"
                      >
                        <span className="cv-improvement-check">
                          <Check
                            size={11}
                            strokeWidth={3}
                          />
                        </span>

                        <p>{improvement}</p>
                      </div>
                    )
                  )}
                </div>
              ) : (
                <p className="cv-no-improvements">
                  No improvement summary was returned.
                </p>
              )}
            </article>

            <article className="cv-download-card">
              <button
                type="button"
                className="cv-download-button cv-download-primary"
                onClick={() => handleDownload("pdf")}
              >
                <Download size={17} />

                <span>Download PDF</span>
              </button>

              <button
                type="button"
                className="cv-download-button cv-download-secondary"
                onClick={() => handleDownload("docx")}
              >
                <Download size={17} />

                <span>Download DOCX</span>
              </button>

              <button
                type="button"
                className="cv-download-button cv-download-neutral"
                onClick={handleTailorAnotherCv}
              >
                <RefreshCcw size={16} />

                <span>Tailor Another CV</span>
              </button>

              {statusMessage && (
                <p
                  className="cv-download-status"
                  role="status"
                >
                  {statusMessage}
                </p>
              )}
            </article>
          </aside>
        </section>
      </>
    );
  }

  return (
    <main className="cv-results-page">
      <header className="cv-results-header">
        <button
          type="button"
          className="cv-results-logo-button"
          onClick={() => navigate("/dashboard")}
          aria-label="Go to dashboard"
        >
          <img
            src={logo}
            alt="DWUMA"
            className="cv-results-logo"
          />
        </button>
      </header>

      <section className="cv-results-body">
        <button
          type="button"
          className="cv-results-menu-button"
          onClick={() =>
            setMenuOpen((currentValue) => !currentValue)
          }
          aria-label={
            menuOpen
              ? "Close navigation menu"
              : "Open navigation menu"
          }
          aria-expanded={menuOpen}
        >
          <Menu size={29} strokeWidth={1.8} />
        </button>

        <button
          type="button"
          className="cv-results-back-button"
          onClick={() =>
            navigate("/dashboard/cv-tailor")
          }
        >
          <ArrowLeft size={16} />

          <span>Back</span>
        </button>

        {menuOpen && (
          <>
            <button
              type="button"
              className="cv-results-menu-overlay"
              onClick={closeMenu}
              aria-label="Close menu"
            />

            <aside className="cv-results-menu">
              <button
                type="button"
                onClick={() =>
                  goToPage("/dashboard")
                }
              >
                <LayoutDashboard size={17} />
                <span>Dashboard</span>
              </button>

              <button
                type="button"
                onClick={() =>
                  goToPage(
                    "/dashboard/interview-coach"
                  )
                }
              >
                <UsersRound size={17} />
                <span>Interview Coach</span>
              </button>

              <button
                type="button"
                onClick={() =>
                  goToPage("/dashboard/cv-tailor")
                }
              >
                <FileText size={17} />
                <span>CV Tailor</span>
              </button>

              <button
                type="button"
                onClick={() =>
                  goToPage("/dashboard/jobs")
                }
              >
                <BriefcaseBusiness size={17} />
                <span>Jobs</span>
              </button>

              <button
                type="button"
                onClick={() =>
                  goToPage("/dashboard/profile")
                }
              >
                <UserRound size={17} />
                <span>Profile</span>
              </button>

              <button
                type="button"
                onClick={() =>
                  goToPage(
                    "/dashboard/notifications"
                  )
                }
              >
                <Bell size={17} />
                <span>Notifications</span>
              </button>

              <button
                type="button"
                onClick={() =>
                  goToPage("/dashboard/settings")
                }
              >
                <Settings size={17} />
                <span>Settings</span>
              </button>
            </aside>
          </>
        )}

        <div className="cv-results-container">
          {renderPageContent()}
        </div>
      </section>
    </main>
  );
}

export default CvTailorResults;