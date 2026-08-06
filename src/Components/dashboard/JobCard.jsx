import {
  BriefcaseBusiness,
  Building2,
  MapPin,
} from "lucide-react";
import { useLocation } from "wouter";

function JobCard({ job }) {
  const [, navigate] = useLocation();

  function handleViewJob() {
    navigate(`/dashboard/jobs/${job.id}`);
  }

  return (
    <article className="recommended-job-card">
      <div className="recommended-job-company-area">
        {job.companyLogoUrl ? (
          <img
            src={job.companyLogoUrl}
            alt={`${job.companyName} logo`}
            className="recommended-job-logo"
          />
        ) : (
          <div
            className="recommended-job-logo-fallback"
            aria-hidden="true"
          >
            <Building2 size={23} />
          </div>
        )}

        <div>
          <h3>{job.title}</h3>
          <p>{job.companyName}</p>
        </div>
      </div>

      <div className="recommended-job-details">
        <span>
          <MapPin size={14} />
          {job.location}
        </span>

        <span>
          <BriefcaseBusiness size={14} />
          {job.employmentType}
        </span>
      </div>

      <span className="recommended-job-arrangement">
        {job.workArrangement}
      </span>

      <button
        type="button"
        className="recommended-job-view-button"
        onClick={handleViewJob}
      >
        View Job
      </button>
    </article>
  );
}

export default JobCard;