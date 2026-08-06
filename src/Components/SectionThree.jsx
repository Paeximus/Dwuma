import image1 from "../assets/image1.png";
import image2 from "../assets/image2.png";
import image3 from "../assets/image3.png";

function SectionThree() {
  const features = [
    {
      image: image1,
      title: "Voice & Text Feature",
      description:
        "You can interact with our AI interview coach via text and voice.",
    },
    {
      image: image2,
      title: "Feedback",
      description:
        "Our AI Interview Coach gives feedback.",
    },
    {
      image: image3,
      title: "Real life Questions",
      description:
        "Practice at your own pace with company and role specific questions.",
    },
  ];

  return (
    <section className="section-three">
      <div className="section-three-header">
        <h2>
          Let us help you
          <br />
          <span>ace that Interview</span>
        </h2>

        <p>
          Say goodbye to the overwhelming job search.
          Dwuma is your AI-powered career agent that searches and
          matches you with the right opportunities tailored to your
          skills and goals.
        </p>
      </div>

      <div className="feature-cards">
        {features.map((feature, index) => (
          <div className="feature-card" key={index}>
            <img
              src={feature.image}
              alt={feature.title}
            />

            <div className="feature-content">
              <h3>{feature.title}</h3>
              <p>{feature.description}</p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

export default SectionThree;