import { useEffect, useMemo, useState } from "react";
import {
  BriefcaseBusiness,
  Building2,
  Funnel,
  MapPin,
  Search,
  SlidersHorizontal,
} from "lucide-react";

import DashboardLayout from "../Components/dashboard/DashboardLayout";
import { getJobs } from "../Components/services/jobsService";
import "./Jobs.css";

function valueOf(job, ...keys) {
  for (const key of keys) {
    if (job?.[key] !== undefined && job[key] !== null) return job[key];
  }
  return "";
}

function Jobs() {
  const [jobs, setJobs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState("");
  const [location, setLocation] = useState("");
  const [jobType, setJobType] = useState("");
  const [sort, setSort] = useState("newest");

  useEffect(() => {
    const controller = new AbortController();

    getJobs({ signal: controller.signal })
      .then(setJobs)
      .catch((error) => {
        if (error.name !== "AbortError") setJobs([]);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, []);

  const locations = useMemo(
    () => [...new Set(jobs.map((job) => valueOf(job, "location", "city")).filter(Boolean))],
    [jobs],
  );
  const jobTypes = useMemo(
    () => [...new Set(jobs.map((job) => valueOf(job, "jobType", "type", "employmentType")).filter(Boolean))],
    [jobs],
  );

  const visibleJobs = useMemo(() => {
    const term = query.trim().toLowerCase();
    const filtered = jobs.filter((job) => {
      const title = valueOf(job, "title", "jobTitle", "position");
      const company = valueOf(job, "company", "companyName", "employer");
      const description = valueOf(job, "description", "summary");
      const place = valueOf(job, "location", "city");
      const type = valueOf(job, "jobType", "type", "employmentType");
      const searchable = `${title} ${company} ${description}`.toLowerCase();
      return (!term || searchable.includes(term)) &&
        (!location || place === location) &&
        (!jobType || type === jobType);
    });

    return filtered.sort((a, b) => {
      const dateA = new Date(valueOf(a, "postedAt", "createdAt", "datePosted") || 0);
      const dateB = new Date(valueOf(b, "postedAt", "createdAt", "datePosted") || 0);
      if (sort === "oldest") return dateA - dateB;
      if (sort === "title") return String(valueOf(a, "title", "jobTitle")).localeCompare(valueOf(b, "title", "jobTitle"));
      return dateB - dateA;
    });
  }, [jobs, query, location, jobType, sort]);

  function resetFilters() {
    setQuery("");
    setLocation("");
    setJobType("");
  }

  return (
    <DashboardLayout>
      <section className="jobs-page">
        <header className="jobs-page-heading">
          <h1>Jobs</h1>
          <p>Discover opportunities that match your skills and career goals.</p>
        </header>

        <div className="jobs-filter-bar">
          <label className="jobs-search-field">
            <span className="sr-only">Search jobs</span>
            <input
              type="search"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Search jobs, companies, or keywords..."
            />
            <Search size={22} aria-hidden="true" />
          </label>

          <select value={location} onChange={(event) => setLocation(event.target.value)} aria-label="Filter by location">
            <option value="">Location</option>
            {locations.map((item) => <option key={item}>{item}</option>)}
          </select>

          <select value={jobType} onChange={(event) => setJobType(event.target.value)} aria-label="Filter by job type">
            <option value="">Job Type</option>
            {jobTypes.map((item) => <option key={item}>{item}</option>)}
          </select>

          <button type="button" className="jobs-filter-button" onClick={resetFilters} aria-label="Clear all job filters" title="Clear filters">
            <SlidersHorizontal size={21} />
          </button>
        </div>

        <div className="jobs-results-toolbar">
          <p>{loading ? "Loading jobs..." : `Showing ${visibleJobs.length} ${visibleJobs.length === 1 ? "job" : "jobs"}`}</p>
          <select value={sort} onChange={(event) => setSort(event.target.value)} aria-label="Sort jobs">
            <option value="newest">Sort by: Newest</option>
            <option value="oldest">Sort by: Oldest</option>
            <option value="title">Sort by: Job title</option>
          </select>
        </div>

        {visibleJobs.length > 0 ? (
          <div className="jobs-results-list">
            {visibleJobs.map((job, index) => {
              const title = valueOf(job, "title", "jobTitle", "position") || "Untitled role";
              const company = valueOf(job, "company", "companyName", "employer") || "Company";
              const place = valueOf(job, "location", "city") || "Location not specified";
              const type = valueOf(job, "jobType", "type", "employmentType") || "Job";
              const url = valueOf(job, "applicationUrl", "applyUrl", "url");
              return (
                <article className="jobs-result-card" key={valueOf(job, "id", "jobId") || `${title}-${index}`}>
                  <div className="jobs-company-mark"><Building2 size={23} /></div>
                  <div className="jobs-result-copy">
                    <h2>{title}</h2>
                    <p className="jobs-company-name">{company}</p>
                    <div className="jobs-result-meta">
                      <span><MapPin size={15} />{place}</span>
                      <span><BriefcaseBusiness size={15} />{type}</span>
                    </div>
                  </div>
                  {url && <a href={url} target="_blank" rel="noreferrer">View job</a>}
                </article>
              );
            })}
          </div>
        ) : (
          <div className="jobs-page-empty">
            <div className="jobs-empty-icon"><Funnel size={30} strokeWidth={2} /></div>
            <h2>{loading ? "Loading jobs" : "No jobs available yet"}</h2>
            <p>{loading ? "Please wait while we find opportunities for you." : "Job opportunities will appear here when the jobs API is connected."}</p>
          </div>
        )}
      </section>
    </DashboardLayout>
  );
}

export default Jobs;
