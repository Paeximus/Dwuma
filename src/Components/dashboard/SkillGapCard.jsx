import { MoreHorizontal } from "lucide-react";

function SkillGapCard({ skills = [] }) {
  const hasSkills = Array.isArray(skills) &&
    skills.length > 0;

  return (
    <article className="dashboard-card skill-card">
      <div className="dashboard-card-header">
        <h2>Skill Gap Analysis</h2>

        <button
          type="button"
          aria-label="Skill card options"
        >
          <MoreHorizontal size={20} />
        </button>
      </div>

      {hasSkills ? (
        <div className="skill-card-content">
          <div className="skill-chart">
            <div className="skill-chart-centre">
              <strong>
                {skills[0]?.percentage ?? 0}%
              </strong>
              <span>Overall</span>
            </div>
          </div>

          <div className="skill-list">
            {skills.slice(0, 3).map((skill) => (
              <div
                className="skill-list-item"
                key={skill.id ?? skill.name}
              >
                <span className="skill-colour" />

                <p>{skill.name}</p>

                <strong>
                  {skill.percentage ?? 0}%
                </strong>
              </div>
            ))}
          </div>
        </div>
      ) : (
        <div className="dashboard-empty-state">
          <div className="empty-donut">
            <span>—</span>
          </div>

          <div>
            <h3>No skills analysis yet</h3>
            <p>
              Your relevant skills and market gaps
              will appear here after your profile is
              analysed.
            </p>
          </div>
        </div>
      )}

      <p className="skill-card-caption">
        Relevant skills in your selected career field
        will appear here.
      </p>
    </article>
  );
}

export default SkillGapCard;