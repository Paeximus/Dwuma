import {
  ArrowRight,
  BriefcaseBusiness,
  Building2,
  MapPin,
} from "lucide-react";

import { useLocation } from "wouter";

function RecommendedJobs({ jobs = [] }) {
  const [, navigate] = useLocation();

  const hasJobs =
    Array.isArray(jobs) && jobs.length > 0;

  function handleViewAll() {
    navigate("/dashboard/jobs");
  }

  function handleViewJob(jobId) {
    navigate(`/dashboard/jobs/${jobId}`);
  }

  return (
    <section className="dashboard-card jobs-card">
      <div className="jobs-card-header">
        <h2>Recommended Jobs</h2>

        <button
          type="button"
          onClick={handleViewAll}
        >
          <span>View all</span>
          <ArrowRight
            size={15}
            aria-hidden="true"
          />
        </button>
      </div>

      {hasJobs ? (
        <div className="jobs-list">
          {jobs.map((job) => (
            <article
              key={job.id}
              className="job-row"
            >
              <div className="job-main-details">
                <div className="job-company-logo">
                  {job.logoUrl ? (
                    <img
                      src={job.logoUrl}
                      alt={`${job.company} logo`}
                    />
                  ) : (
                    <Building2
                      size={20}
                      aria-hidden="true"
                    />
                  )}
                </div>

                <div>
                  <h3>{job.title}</h3>
                  <p>{job.company}</p>
                </div>
              </div>

              <div className="job-location-details">
                <span>
                  <MapPin
                    size={13}
                    aria-hidden="true"
                  />

                  {job.location}
                </span>

                <span>
                  <BriefcaseBusiness
                    size={13}
                    aria-hidden="true"
                  />

                  {job.type}
                </span>
              </div>

              {job.workMode && (
                <span className="job-work-mode">
                  {job.workMode}
                </span>
              )}

              <button
                type="button"
                className="job-view-button"
                onClick={() =>
                  handleViewJob(job.id)
                }
              >
                View job
              </button>
            </article>
          ))}
        </div>
      ) : (
        <div className="jobs-empty-state">
          <div className="jobs-empty-icon">
            <BriefcaseBusiness
              size={30}
              aria-hidden="true"
            />
          </div>

          <h3>No recommended jobs available</h3>

          <p>
            Job recommendations will appear here when
            they are returned by the backend.
          </p>
        </div>
      )}
    </section>
  );
}

export default RecommendedJobs;