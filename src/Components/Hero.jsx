import lady from "../assets/lady.svg";

function Hero() {
  return (
    <section className="hero">

      <div className="hero-content">
        <h1>
          Graduate job
          <br />
          search,
          <br />
          <span>reimagined</span>
        </h1>

        <p>Dwuma has got you</p>
      </div>

      <div className="hero-image">
        <img src={lady} alt="Graduate job search illustration" />
      </div>

    </section>
  );
}

export default Hero;