import { useState } from "react";
import { useLocation } from "wouter";
import "./CareerPreferences.css";

const JOB_FORMS = ["Hybrid", "Remote", "Onsite"];

const WORK_FIELDS = [
  "Accounting and Finance",
  "Administration",
  "Agriculture",
  "Architecture",
  "Arts and Design",
  "Business and Management",
  "Construction",
  "Customer Service",
  "Data and Analytics",
  "Education",
  "Engineering",
  "Healthcare",
  "Hospitality and Tourism",
  "Human Resources",
  "Information Technology",
  "Law",
  "Marketing and Communications",
  "Media and Entertainment",
  "Project Management",
  "Sales",
  "Science and Research",
  "Software Development",
  "Supply Chain and Procurement",
];

const AVAILABLE_SKILLS = [
  "Accounting",
  "Adobe Illustrator",
  "Adobe Photoshop",
  "Auditing",
  "Business Analysis",
  "Cloud Computing",
  "Communication",
  "Content Creation",
  "Customer Service",
  "Cybersecurity",
  "Data Analysis",
  "Digital Marketing",
  "Financial Reporting",
  "Graphic Design",
  "Java",
  "JavaScript",
  "Leadership",
  "Microsoft Excel",
  "Microsoft Office",
  "Networking",
  "Problem Solving",
  "Project Management",
  "Python",
  "React",
  "Sales",
  "Social Media Management",
  "SQL",
  "Teamwork",
  "UI/UX Design",
  "Web Development",
];

function OnboardingStepTwo() {
  const [, navigate] = useLocation();

  const [formData, setFormData] = useState({
    jobForm: "",
    fieldOfWork: "",
    skills: [],
  });

  const [skillsOpen, setSkillsOpen] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const isFormComplete =
    formData.jobForm !== "" &&
    formData.fieldOfWork !== "" &&
    formData.skills.length > 0;

  function selectJobForm(jobForm) {
    setFormData((currentData) => ({
      ...currentData,
      jobForm,
    }));

    setErrorMessage("");
  }

  function handleFieldChange(event) {
    setFormData((currentData) => ({
      ...currentData,
      fieldOfWork: event.target.value,
    }));

    setErrorMessage("");
  }

  function toggleSkill(skill) {
    setFormData((currentData) => {
      const skillIsSelected = currentData.skills.includes(skill);

      return {
        ...currentData,
        skills: skillIsSelected
          ? currentData.skills.filter(
              (selectedSkill) => selectedSkill !== skill
            )
          : [...currentData.skills, skill],
      };
    });

    setErrorMessage("");
  }

  function removeSkill(skill) {
    setFormData((currentData) => ({
      ...currentData,
      skills: currentData.skills.filter(
        (selectedSkill) => selectedSkill !== skill
      ),
    }));
  }

  function handleSubmit(event) {
    event.preventDefault();

    if (!isFormComplete) {
      setErrorMessage(
        "Please select your job form, field of work and at least one skill."
      );
      return;
    }

    let stepOneData = {};

    try {
      const savedStepOneData = localStorage.getItem(
        "dwumaCareerPreferences"
      );

      if (savedStepOneData) {
        stepOneData = JSON.parse(savedStepOneData);
      }
    } catch {
      stepOneData = {};
    }

    const completeOnboardingData = {
      ...stepOneData,
      ...formData,
    };

    localStorage.setItem(
      "dwumaOnboardingData",
      JSON.stringify(completeOnboardingData)
    );

    localStorage.setItem("dwumaOnboardingComplete", "true");

    navigate("/dashboard");
  }

  function goBack() {
    navigate("/onboarding/step-1");
  }

  return (
    <main className="career-page">
      <section className="career-card">
        <div className="career-progress-section">
          <p className="career-step">Step 2 of 2</p>

          <div
            className="career-progress-track"
            aria-label="Onboarding progress"
          >
            <div className="career-progress-value step-two-progress" />
          </div>
        </div>

        <div className="career-heading">
          <h1>Tell us more about your work preferences</h1>

          <p>
            Select how you would like to work, your professional field
            and the skills you currently have.
          </p>
        </div>

        <form className="career-form" onSubmit={handleSubmit}>
          <fieldset className="job-type-section">
            <legend>Job form</legend>

            <div className="job-type-options">
              {JOB_FORMS.map((jobForm) => (
                <button
                  key={jobForm}
                  type="button"
                  className={`job-type-button ${
                    formData.jobForm === jobForm
                      ? "job-type-button-selected"
                      : ""
                  }`}
                  onClick={() => selectJobForm(jobForm)}
                  aria-pressed={formData.jobForm === jobForm}
                >
                  {formData.jobForm === jobForm && (
                    <span className="selected-check">✓</span>
                  )}

                  {jobForm}
                </button>
              ))}
            </div>
          </fieldset>

          <div className="career-field">
            <label htmlFor="fieldOfWork">Field of work</label>

            <div className="select-wrapper">
              <select
                id="fieldOfWork"
                name="fieldOfWork"
                value={formData.fieldOfWork}
                onChange={handleFieldChange}
                required
              >
                <option value="" disabled>
                  Select your field of work
                </option>

                {WORK_FIELDS.map((field) => (
                  <option key={field} value={field}>
                    {field}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="career-field skills-field">
            <label id="skills-label">Skills</label>

            <button
              type="button"
              className={`skills-select-button ${
                skillsOpen ? "skills-select-button-open" : ""
              }`}
              onClick={() => setSkillsOpen((current) => !current)}
              aria-expanded={skillsOpen}
              aria-labelledby="skills-label"
            >
              <span>
                {formData.skills.length === 0
                  ? "Select your skills"
                  : `${formData.skills.length} skill${
                      formData.skills.length > 1 ? "s" : ""
                    } selected`}
              </span>

              <span
                className={`skills-arrow ${
                  skillsOpen ? "skills-arrow-open" : ""
                }`}
              >
               ⌄
              </span>
            </button>

            {skillsOpen && (
              <div className="skills-dropdown">
                {AVAILABLE_SKILLS.map((skill) => {
                  const isSelected =
                    formData.skills.includes(skill);

                  return (
                    <label
                      key={skill}
                      className="skill-checkbox-option"
                    >
                      <input
                        type="checkbox"
                        checked={isSelected}
                        onChange={() => toggleSkill(skill)}
                      />

                      <span>{skill}</span>
                    </label>
                  );
                })}
              </div>
            )}

            {formData.skills.length > 0 && (
              <div className="selected-skills">
                {formData.skills.map((skill) => (
                  <span className="selected-skill-tag" key={skill}>
                    {skill}

                    <button
                      type="button"
                      onClick={() => removeSkill(skill)}
                      aria-label={`Remove ${skill}`}
                    >
                      ×
                    </button>
                  </span>
                ))}
              </div>
            )}
          </div>

          {errorMessage && (
            <p className="career-error" role="alert">
              {errorMessage}
            </p>
          )}

          <div className="onboarding-buttons">
            <button
              type="button"
              className="career-back-button"
              onClick={goBack}
            >
              Back
            </button>

            <button
              type="submit"
              className="career-next-button"
              disabled={!isFormComplete}
            >
              Finish
            </button>
          </div>

          <p className="career-privacy-note">
            Your information will be used to personalise job
            recommendations, skill-gap analysis, CV assistance and
            interview coaching.
          </p>
        </form>
      </section>
    </main>
  );
}

export default OnboardingStepTwo;