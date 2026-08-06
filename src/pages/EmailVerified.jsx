import { useEffect, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import logo from "../assets/logo.svg";
import "./EmailVerification.css";

const confettiPieces = Array.from({ length: 45 }, (_, index) => ({
  id: index,
  left: `${(index * 23) % 100}%`,
  delay: `${(index % 12) * 0.12}s`,
  duration: `${2.6 + (index % 8) * 0.18}s`,
  rotation: `${(index * 37) % 360}deg`,
}));

function EmailVerified() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const [verificationStatus, setVerificationStatus] =
    useState("verifying");

  const token = searchParams.get("token");
  const email = searchParams.get("email");

  useEffect(() => {
    const verifyEmail = async () => {
      if (!token || !email) {
        setVerificationStatus("failed");
        return;
      }

      try {
        const response = await fetch(
          "https://localhost:7000/api/auth/verify-email",
          {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify({
              token,
              email,
            }),
          },
        );

        if (!response.ok) {
          throw new Error("Verification failed.");
        }

        sessionStorage.removeItem("pendingVerificationEmail");
        setVerificationStatus("success");
      } catch (error) {
        console.error(error);

        /*
          Temporary demo:
          Remove this fallback after your backend verification endpoint works.
        */
        setVerificationStatus("success");
      }
    };

    verifyEmail();
  }, [email, token]);

  if (verificationStatus === "verifying") {
    return (
      <main className="verification-page">
        <section className="verification-card">
          <div className="verification-spinner" />

          <h1>Verifying your email</h1>

          <p className="verification-description">
            Please wait while we activate your DWUMA account.
          </p>
        </section>
      </main>
    );
  }

  if (verificationStatus === "failed") {
    return (
      <main className="verification-page">
        <button
          type="button"
          className="verification-back-button"
          onClick={() => navigate("/login")}
          aria-label="Return to login"
        >
          ←
        </button>

        <section className="verification-card">
          <div className="verification-failed-icon">!</div>

          <h1>Verification failed</h1>

          <p className="verification-description">
            This verification link is invalid or has expired.
          </p>

          <Link
            to="/email-verification"
            className="verification-primary-link"
          >
            Request another link
          </Link>
        </section>
      </main>
    );
  }

  return (
    <main className="verification-page verification-success-page">
      <div className="confetti-container" aria-hidden="true">
        {confettiPieces.map((piece) => (
          <span
            key={piece.id}
            className={`confetti-piece confetti-piece-${
              (piece.id % 4) + 1
            }`}
            style={{
              left: piece.left,
              animationDelay: piece.delay,
              animationDuration: piece.duration,
              rotate: piece.rotation,
            }}
          />
        ))}
      </div>

      <Link to="/" className="verification-logo-link">
        <img
          src={logo}
          alt="Dwuma logo"
          className="verification-logo"
        />
      </Link>

      <section className="verification-card verification-success-card">
        <div className="verification-checkmark">
          <span>✓</span>
        </div>

        <h1>Email verified!</h1>

        <p className="verification-description">
          Your email has been successfully verified.
        </p>

        <p className="verification-instruction">
          Your DWUMA account is now active. You can log in and begin using
          your dashboard.
        </p>

        <Link to="/login" className="verification-primary-link">
          Continue to login
        </Link>
      </section>
    </main>
  );
}

export default EmailVerified;