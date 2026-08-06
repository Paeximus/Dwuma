import lady2 from "../assets/lady2.png";

function SectionFour() {
  return (
    <section className="section-four">
      <div className="section-four-container">

        <div className="section-four-text">

          <h2>
            Better that <span>RESUME</span>
          </h2>

          <p>
            Say goodbye to the overwhelming job search.
            Dwuma is your AI-powered career agent that
            searches and matches you with the right opportunities.
          </p>

          <h3>Building your expertise</h3>

          <p>
            Say goodbye to the overwhelming job search.
            Dwuma is your AI-powered career agent that searches
            and matches you with the right opportunities tailored
            to your skills and goals.
          </p>

          <button className="resume-btn">
            Resume tailor
          </button>

        </div>

        <div className="section-four-image">
          <img src={lady2} alt="Resume Tailor" />
        </div>

        <div className="upload-circle">
          <div className="upload-icon">
            ↑
          </div>
        </div>

      </div>
    </section>
  );
}

export default SectionFour;