import findJob from "../assets/findjob.svg";

function SectionTwo() {
  return (
    <section className="section-two">
      <div className="section-two-content">

        <div className="section-two-text">
          <h4>Find jobs on</h4>

          <h2>DWUMA</h2>

          <p>
            Say goodbye to the overwhelming job search.
            Dwuma is your AI-powered career agent that
            searches and matches you with the right jobs.
          </p>

          <div className="section-buttons">
            <button className="primary-btn">
              Sign up
            </button>

            <button className="secondary-btn">
              Learn More
            </button>
          </div>
        </div>

        <div className="section-two-image">
          <img src={findJob} alt="Find Jobs Illustration" />
        </div>

      </div>

      <div className="stats-row">
        {[...Array(10)].map((_, index) => (
          <div key={index} className="stat-item">
            <span>100+</span>
            <p>students</p>
          </div>
        ))}
      </div>
    </section>
  );
}

export default SectionTwo;