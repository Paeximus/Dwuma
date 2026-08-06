import { useState } from "react";
import { Link, useLocation } from "wouter";

import logo from "../assets/logo.svg";
import "./EmailVerification.css";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ||
  "https://localhost:7000/api";

function EmailVerification() {
  const [, navigate] = useLocation();

  const [isResending, setIsResending] = useState(false);
  const [message, setMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  const email =
    sessionStorage.getItem("pendingVerificationEmail") ||
    "your email address";

  async function handleResendEmail() {
    if (email === "your email address") {
      setErrorMessage(
        "No email address was found. Please create your account again.",
      );
      return;
    }

    try {
      setIsResending(true);
      setMessage("");
      setErrorMessage("");

      const response = await fetch(
        `${API_BASE_URL}/auth/resend-verification`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({ email }),
        },
      );

      const data = await response.json().catch(() => null);

      if (!response.ok) {
        throw new Error(
          data?.message ||
            data?.title ||
            "Unable to resend the verification email.",
        );
      }

      setMessage("A new verification link has been sent.");
    } catch (error) {
      console.error("Resend verification error:", error);

      setErrorMessage(
        error instanceof Error
          ? error.message
          : "Unable to resend the verification email.",
      );
    } finally {
      setIsResending(false);
    }
  }

  return (
    <main className="verification-page">
      <button
        type="button"
        className="verification-back-button"
        onClick={() => navigate("/create-account")}
        aria-label="Return to create account"
      >
        ←
      </button>

      <Link href="/" className="verification-logo-link">
        <img
          src={logo}
          alt="Dwuma logo"
          className="verification-logo"
        />
      </Link>

      <section className="verification-card">
        <div className="email-icon" aria-hidden="true">
          <span>✉</span>
        </div>

        <h1>Verify your email</h1>

        <p className="verification-description">
          We sent a verification link to
        </p>

        <p className="verification-email">{email}</p>

        <p className="verification-instruction">
          Open the email and click the verification link to activate your
          DWUMA account.
        </p>

        {message && (
          <p className="verification-success" role="status">
            {message}
          </p>
        )}

        {errorMessage && (
          <p className="verification-error" role="alert">
            {errorMessage}
          </p>
        )}

        <button
          type="button"
          className="resend-verification-button"
          onClick={handleResendEmail}
          disabled={isResending}
        >
          {isResending ? "Sending..." : "Resend email"}
        </button>

        <p className="verification-help">
          Already verified? <Link href="/login">Go to login</Link>
        </p>
      </section>
    </main>
  );
}

export default EmailVerification;