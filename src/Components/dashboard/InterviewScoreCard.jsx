import { MoreHorizontal } from "lucide-react";
import { useLocation } from "wouter";

import interviewImage from "../../assets/interview.svg";

function InterviewScoreCard({ interview }) {
  const [, navigate] = useLocation();

  const hasInterview =
    interview?.completed === true &&
    typeof interview?.score === "number";

  return (
    <article className="dashboard-card interview-card">
      <div className="dashboard-card-header interview-card-header">
        <h2>Interview Score</h2>

        <button
          type="button"
          aria-label="Interview card options"
        >
          <MoreHorizontal size={20} />
        </button>
      </div>

      <div className="interview-card-body">
        {hasInterview ? (
          <>
            <div
              className="interview-score-ring"
              style={{
                "--score": `${
                  Math.min(
                    Math.max(interview.score, 0),
                    100,
                  ) * 3.6
                }deg`,
              }}
            >
              <div>
                <strong>
                  {interview.score}%
                </strong>
                <span>Your score</span>
              </div>
            </div>

            <div className="interview-card-message">
              <h3>
                {interview.message ??
                  "Interview completed"}
              </h3>

              <p>
                {interview.feedback ??
                  "Your interview feedback will appear here."}
              </p>
            </div>
          </>
        ) : (
          <div className="interview-empty-content">
            <div className="empty-score-ring">
              <strong>—</strong>
              <span>No score</span>
            </div>

            <div>
              <h3>Complete an interview</h3>

              <p>
                Your latest interview score and
                feedback will appear here.
              </p>

              <button
                type="button"
                onClick={() =>
                  navigate(
                    "/dashboard/interview-coach",
                  )
                }
              >
                Start interview
              </button>
            </div>
          </div>
        )}

        <img
          src={interviewImage}
          alt=""
          className="interview-card-image"
        />
      </div>
    </article>
  );
}

export default InterviewScoreCard;