import { useRef, useState } from "react";
import { useLocation } from "wouter";
import {
  Bell,
  BriefcaseBusiness,
  Check,
  FileText,
  LayoutDashboard,
  Menu,
  Settings,
  Upload,
  UserRound,
  UsersRound,
  WandSparkles,
} from "lucide-react";

import logo from "../assets/logo.svg";
import "./CvTailor.css";

const tailoringOptions = [
  {
    id: "grammar",
    label: "Improve grammar & clarity",
  },
  {
    id: "summary",
    label: "Professional summary boost",
  },
  {
    id: "ats",
    label: "ATS optimization",
  },
  {
    id: "match-job",
    label: "Match job description",
  },
  {
    id: "action-verbs",
    label: "Stronger action verbs",
  },
  {
    id: "strengths",
    label: "Highlight key strengths",
  },
  {
    id: "achievements",
    label: "Enhance achievements",
  },
  {
    id: "remove-content",
    label: "Remove irrelevant content",
  },
  {
    id: "skills",
    label: "Optimize skills section",
  },
  {
    id: "graduate-focus",
    label: "Graduate job focus",
  },
];

function createDefaultOptions() {
  return tailoringOptions.reduce((options, item) => {
    options[item.id] = true;
    return options;
  }, {});
}

function CvTailor() {
  const [, navigate] = useLocation();
  const fileInputRef = useRef(null);

  const [menuOpen, setMenuOpen] = useState(false);
  const [selectedFile, setSelectedFile] = useState(null);
  const [jobDescription, setJobDescription] = useState("");
  const [isDragging, setIsDragging] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");
  const [selectedOptions, setSelectedOptions] =
    useState(createDefaultOptions);

  function closeMenu() {
    setMenuOpen(false);
  }

  function goToPage(path) {
    closeMenu();
    navigate(path);
  }

  function openFilePicker() {
    fileInputRef.current?.click();
  }

  function validateAndSaveFile(file) {
    if (!file) {
      return;
    }

    const allowedExtensions = ["pdf", "doc", "docx"];

    const extension = file.name
      .split(".")
      .pop()
      ?.toLowerCase();

    const maximumFileSize = 10 * 1024 * 1024;

    if (!extension || !allowedExtensions.includes(extension)) {
      setSelectedFile(null);

      setStatusMessage(
        "Please upload a PDF, DOC or DOCX file."
      );

      return;
    }

    if (file.size > maximumFileSize) {
      setSelectedFile(null);

      setStatusMessage(
        "The selected file is larger than 10MB."
      );

      return;
    }

    setSelectedFile(file);
    setStatusMessage("");
  }

  function handleFileChange(event) {
    const file = event.target.files?.[0];

    validateAndSaveFile(file);

    event.target.value = "";
  }

  function handleDragOver(event) {
    event.preventDefault();
    setIsDragging(true);
  }

  function handleDragLeave(event) {
    event.preventDefault();
    setIsDragging(false);
  }

  function handleDrop(event) {
    event.preventDefault();
    setIsDragging(false);

    const file = event.dataTransfer.files?.[0];

    validateAndSaveFile(file);
  }

  function toggleOption(optionId) {
    setSelectedOptions((currentOptions) => ({
      ...currentOptions,
      [optionId]: !currentOptions[optionId],
    }));
  }

  function resetCvTailor() {
    setSelectedFile(null);
    setJobDescription("");
    setSelectedOptions(createDefaultOptions());
    setStatusMessage("");

    sessionStorage.removeItem(
      "dwumaCvTailorRequest"
    );

    sessionStorage.removeItem(
      "dwumaCvTailorResult"
    );
  }

  function handleTailorCv() {
    if (!selectedFile) {
      setStatusMessage(
        "Upload your CV before tailoring it."
      );

      return;
    }

    const enabledOptions = Object.entries(
      selectedOptions
    )
      .filter(([, enabled]) => enabled)
      .map(([optionId]) => optionId);

    if (enabledOptions.length === 0) {
      setStatusMessage(
        "Select at least one tailoring preference."
      );

      return;
    }

    const cvTailorRequest = {
      fileName: selectedFile.name,
      fileSize: selectedFile.size,
      fileType: selectedFile.type,
      jobDescription: jobDescription.trim(),
      preferences: enabledOptions,
      submittedAt: new Date().toISOString(),
    };

    /*
      Temporary frontend storage.

      Only the upload details are stored. The actual CV
      file is not stored in sessionStorage.
    */
    sessionStorage.setItem(
      "dwumaCvTailorRequest",
      JSON.stringify(cvTailorRequest)
    );

    sessionStorage.removeItem(
      "dwumaCvTailorResult"
    );

    console.log("CV tailor request:", {
      file: selectedFile,
      ...cvTailorRequest,
    });

    /*
      Add the backend connection later:

      const formData = new FormData();

      formData.append("cv", selectedFile);
      formData.append(
        "jobDescription",
        jobDescription.trim()
      );
      formData.append(
        "preferences",
        JSON.stringify(enabledOptions)
      );

      const response = await fetch(
        `${API_BASE_URL}/cv-tailor/upload`,
        {
          method: "POST",
          body: formData,
        }
      );

      if (!response.ok) {
        throw new Error(
          "Unable to tailor the uploaded CV."
        );
      }

      const result = await response.json();

      sessionStorage.setItem(
        "dwumaCvTailorResult",
        JSON.stringify(result)
      );
    */

    setStatusMessage("");

    navigate("/dashboard/cv-tailor/results");
  }

  return (
    <main className="cv-tailor-screen">
      <header className="cv-tailor-header">
        <button
          type="button"
          className="cv-tailor-logo-button"
          onClick={() => navigate("/dashboard")}
          aria-label="Go to dashboard"
        >
          <img
            src={logo}
            alt="DWUMA"
            className="cv-tailor-logo"
          />
        </button>
      </header>

      <section className="cv-tailor-body">
        <button
          type="button"
          className="cv-tailor-menu-button"
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

        {menuOpen && (
          <>
            <button
              type="button"
              className="cv-tailor-menu-overlay"
              onClick={closeMenu}
              aria-label="Close menu"
            />

            <aside className="cv-tailor-menu">
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
                className="cv-tailor-menu-active"
                onClick={closeMenu}
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

        <div className="cv-tailor-content">
          <div className="cv-tailor-heading">
            <h1>Better That Resume</h1>

            <p>
              Upload your CV and tailor it for the role
              you are applying for.
            </p>
          </div>

          <section
            className={`cv-upload-card ${
              isDragging
                ? "cv-upload-card-dragging"
                : ""
            }`}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
          >
            <div className="cv-upload-inner">
              <div className="cv-upload-icon-wrapper">
                <Upload
                  className="cv-upload-icon"
                  size={42}
                  strokeWidth={1.8}
                />
              </div>

              <h2>
                {selectedFile
                  ? selectedFile.name
                  : "Drag & drop your CV here"}
              </h2>

              <p>
                {selectedFile
                  ? "Your CV has been selected successfully."
                  : "or click the button below to browse your files"}
              </p>

              <input
                ref={fileInputRef}
                type="file"
                accept=".pdf,.doc,.docx"
                onChange={handleFileChange}
                hidden
              />

              <div className="cv-upload-actions">
                <button
                  type="button"
                  className="cv-upload-button"
                  onClick={openFilePicker}
                >
                  <Upload size={17} strokeWidth={2.2} />

                  <span>
                    {selectedFile
                      ? "Change CV"
                      : "Upload CV"}
                  </span>
                </button>

                {selectedFile && (
                  <button
                    type="button"
                    className="cv-remove-file-button"
                    onClick={() => {
                      setSelectedFile(null);
                      setStatusMessage("");
                    }}
                  >
                    Remove
                  </button>
                )}
              </div>

              <small>
                Supported formats: PDF, DOCX and DOC.
                Maximum size: 10MB.
              </small>
            </div>
          </section>

          <div className="cv-tailor-grid">
            <section className="cv-tailor-card">
              <div className="cv-card-heading">
                <span className="cv-card-icon">
                  <FileText
                    size={22}
                    strokeWidth={2}
                  />
                </span>

                <div>
                  <h2>Job Description</h2>

                  <p>
                    Paste the job description to tailor
                    your CV for a specific role.
                  </p>
                </div>
              </div>

              <textarea
                value={jobDescription}
                onChange={(event) =>
                  setJobDescription(
                    event.target.value.slice(0, 2000)
                  )
                }
                placeholder="Paste the job description here..."
                maxLength={2000}
              />

              <span className="cv-character-count">
                {jobDescription.length}/2000 characters
              </span>
            </section>

            <section className="cv-tailor-card">
              <div className="cv-card-heading">
                <span className="cv-card-icon">
                  <WandSparkles
                    size={22}
                    strokeWidth={2}
                  />
                </span>

                <div>
                  <h2>Tailoring Preferences</h2>

                  <p>
                    Select the improvements you want
                    applied to your CV.
                  </p>
                </div>
              </div>

              <div className="cv-options-grid">
                {tailoringOptions.map((option) => {
                  const isSelected =
                    selectedOptions[option.id];

                  return (
                    <button
                      key={option.id}
                      type="button"
                      className={`cv-option ${
                        isSelected
                          ? "cv-option-selected"
                          : ""
                      }`}
                      onClick={() =>
                        toggleOption(option.id)
                      }
                      aria-pressed={isSelected}
                    >
                      <span
                        className={`cv-option-checkbox ${
                          isSelected
                            ? "cv-option-checkbox-selected"
                            : ""
                        }`}
                      >
                        {isSelected && (
                          <Check
                            size={13}
                            strokeWidth={3}
                          />
                        )}
                      </span>

                      <span>{option.label}</span>
                    </button>
                  );
                })}
              </div>
            </section>
          </div>

          {statusMessage && (
            <p
              className="cv-tailor-status"
              role="status"
            >
              {statusMessage}
            </p>
          )}

          <div className="cv-tailor-bottom-actions">
            <button
              type="button"
              className="cv-tailor-reset-button"
              onClick={resetCvTailor}
            >
              Clear
            </button>

            <button
              type="button"
              className="cv-tailor-submit-button"
              onClick={handleTailorCv}
            >
              Tailor My CV
            </button>
          </div>
        </div>
      </section>
    </main>
  );
}

export default CvTailor;