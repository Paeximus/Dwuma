import { useState } from "react";
import { useLocation } from "wouter";
import "./CareerPreferences.css";

const JOB_TYPES = [
  "Full-time",
  "Part-time",
  "Freelance/Contract",
];

const SALARY_RANGES = [
  "Less than GHS 2,000/month",
  "GHS 2,000 – 3,999/month",
  "GHS 4,000 – 5,999/month",
  "GHS 6,000 – 7,999/month",
  "GHS 8,000 – 9,999/month",
  "GHS 10,000 – 14,999/month",
  "GHS 15,000+/month",
  "Negotiable",
];

const JOB_ROLES = [
  "Software Engineer",
  "Frontend Developer",
  "Backend Developer",
  "Full Stack Developer",
  "Mobile App Developer",
  "Data Analyst",
  "Data Scientist",
  "Artificial Intelligence Engineer",
  "Cybersecurity Analyst",
  "Network Engineer",
  "Cloud Engineer",
  "DevOps Engineer",
  "UI/UX Designer",
  "Graphic Designer",
  "Product Manager",
  "Project Manager",
  "Business Analyst",
  "Accountant",
  "Financial Analyst",
  "Human Resource Officer",
  "Marketing Officer",
  "Sales Executive",
  "Customer Service Representative",
  "Administrative Officer",
  "Procurement Officer",
  "Civil Engineer",
  "Mechanical Engineer",
  "Electrical Engineer",
  "Quantity Surveyor",
  "Registered Nurse",
  "Medical Laboratory Scientist",
  "Pharmacist",
  "Teacher",
  "Lecturer",
];

function CareerPreferences() {
  const [, navigate] = useLocation();

  const [formData, setFormData] = useState({
    jobType: "",
    salaryRange: "",
    desiredRole: "",
  });

  const [errorMessage, setErrorMessage] = useState("");

  const isFormComplete =
    formData.jobType &&
    formData.salaryRange &&
    formData.desiredRole;

  function selectJobType(jobType) {
    setFormData((currentData) => ({
      ...currentData,
      jobType,
    }));

    setErrorMessage("");
  }

  function handleSelectChange(event) {
    const { name, value } = event.target;

    setFormData((currentData) => ({
      ...currentData,
      [name]: value,
    }));

    setErrorMessage("");
  }

  function handleSubmit(event) {
    event.preventDefault();

    if (!isFormComplete) {
      setErrorMessage(
        "Please select your job type, salary range and desired role."
      );
      return;
    }

    localStorage.setItem(
      "dwumaCareerPreferences",
      JSON.stringify(formData)
    );

    navigate("/onboarding/step-2");
  }

  return (
    <main className="career-page">
      <section className="career-card">
        <div className="career-progress-section">
          <p className="career-step">Step 1 of 2</p>

          <div
            className="career-progress-track"
            aria-label="Onboarding progress"
          >
            <div className="career-progress-value" />
          </div>
        </div>

        <div className="career-heading">
          <h1>What role do you want to find?</h1>

          <p>
            Tell us about the job you&apos;re targeting so we can
            tailor your search and recommendations.
          </p>
        </div>

        <form className="career-form" onSubmit={handleSubmit}>
          <fieldset className="job-type-section">
            <legend>Job type</legend>

            <div className="job-type-options">
              {JOB_TYPES.map((jobType) => (
                <button
                  key={jobType}
                  type="button"
                  className={`job-type-button ${
                    formData.jobType === jobType
                      ? "job-type-button-selected"
                      : ""
                  }`}
                  onClick={() => selectJobType(jobType)}
                  aria-pressed={formData.jobType === jobType}
                >
                  {formData.jobType === jobType && (
                    <span className="selected-check">✓</span>
                  )}

                  {jobType}
                </button>
              ))}
            </div>
          </fieldset>

          <div className="career-field">
            <label htmlFor="salaryRange">
              Expected salary range
            </label>

            <div className="select-wrapper">
              <select
                id="salaryRange"
                name="salaryRange"
                value={formData.salaryRange}
                onChange={handleSelectChange}
                required
              >
                <option value="" disabled>
                  Select expected salary
                </option>

                {SALARY_RANGES.map((salary) => (
                  <option key={salary} value={salary}>
                    {salary}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="career-field">
            <label htmlFor="desiredRole">Your role</label>

            <div className="select-wrapper">
              <select
                id="desiredRole"
                name="desiredRole"
                value={formData.desiredRole}
                onChange={handleSelectChange}
                required
              >
                <option value="" disabled>
                  Select your desired role
                </option>

                {JOB_ROLES.map((role) => (
                  <option key={role} value={role}>
                    {role}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {errorMessage && (
            <p className="career-error" role="alert">
              {errorMessage}
            </p>
          )}

          <button
            type="submit"
            className="career-next-button"
            disabled={!isFormComplete}
          >
            Next
          </button>

          <p className="career-privacy-note">
            We use this information to personalise your job
            recommendations, CV improvements, interview coaching and
            skill-gap analysis.
          </p>
        </form>
      </section>
    </main>
  );
}

export default CareerPreferences;