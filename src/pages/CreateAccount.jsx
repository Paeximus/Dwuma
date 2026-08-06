import { useState } from "react";
import { Link, useLocation } from "wouter";

// import ReCAPTCHA from "react-google-recaptcha";

import signupImage from "../assets/signup.svg";
import logo from "../assets/logo.svg";
import "./CreateAccount.css";

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ||
  "https://localhost:7001/api";

function CreateAccount() {
  const [, navigate] = useLocation();

  // const recaptchaRef = useRef(null);

  const [formData, setFormData] = useState({
    username: "",
    email: "",
    password: "",
    confirmPassword: "",
  });

  // const [recaptchaToken, setRecaptchaToken] =
  //   useState("");

  const [showPassword, setShowPassword] =
    useState(false);

  const [
    showConfirmPassword,
    setShowConfirmPassword,
  ] = useState(false);

  const [errorMessage, setErrorMessage] =
    useState("");

  const [isSubmitting, setIsSubmitting] =
    useState(false);

  function handleChange(event) {
    const { name, value } = event.target;

    setFormData((currentData) => ({
      ...currentData,
      [name]: value,
    }));

    setErrorMessage("");
  }

  function validateForm() {
    const username = formData.username.trim();
    const email = formData.email
      .trim()
      .toLowerCase();

    if (
      !username ||
      !email ||
      !formData.password ||
      !formData.confirmPassword
    ) {
      return "Please complete all required fields.";
    }

    if (username.length < 3) {
      return "Username must contain at least 3 characters.";
    }

    const emailPattern =
      /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    if (!emailPattern.test(email)) {
      return "Please enter a valid email address.";
    }

    if (formData.password.length < 8) {
      return "Password must contain at least 8 characters.";
    }

    if (
      formData.password !==
      formData.confirmPassword
    ) {
      return "The passwords do not match.";
    }

    /*
    if (!recaptchaToken) {
      return "Please complete the reCAPTCHA verification.";
    }
    */

    return "";
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setErrorMessage("");

    const validationError = validateForm();

    if (validationError) {
      setErrorMessage(validationError);
      return;
    }

    setIsSubmitting(true);

    const username = formData.username.trim();

    const email = formData.email
      .trim()
      .toLowerCase();

    try {
      const response = await fetch(
        `${API_BASE_URL}/auth/register`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            username,
            email,
            password: formData.password,
            confirmPassword:
              formData.confirmPassword,

            // recaptchaToken,
          }),
        },
      );

      const data = await response
        .json()
        .catch(() => null);

      if (!response.ok) {
        throw new Error(
          data?.message ||
            data?.title ||
            "Unable to create your account.",
        );
      }

      sessionStorage.setItem(
        "pendingVerificationEmail",
        email,
      );

      navigate("/email-verification", {
        replace: true,
      });
    } catch (error) {
      console.error(
        "Account creation error:",
        error,
      );

      setErrorMessage(
        error instanceof Error
          ? error.message
          : "Unable to create your account. Please try again.",
      );

      /*
      recaptchaRef.current?.reset();
      setRecaptchaToken("");
      */
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="create-account-page">
      <section className="create-account-layout">
        <div className="create-account-illustration">
          <Link
            href="/"
            className="create-account-logo-link"
          >
            <img
              src={logo}
              alt="Dwuma logo"
              className="create-account-logo"
            />
          </Link>

          <img
            src={signupImage}
            alt="People working together"
            className="create-account-image"
          />
        </div>

        <div className="create-account-panel">
          <div className="create-account-form-wrapper">
            <header className="create-account-header">
              <h1>Create account</h1>
              <p>Welcome to DWUMA</p>
            </header>

            <form
              className="create-account-form"
              onSubmit={handleSubmit}
              noValidate
            >
              <div className="create-account-group">
                <label htmlFor="signup-username">
                  Username
                </label>

                <input
                  id="signup-username"
                  name="username"
                  type="text"
                  value={formData.username}
                  onChange={handleChange}
                  autoComplete="username"
                  placeholder="Enter your username"
                  disabled={isSubmitting}
                />
              </div>

              <div className="create-account-group">
                <label htmlFor="signup-email">
                  Email
                </label>

                <input
                  id="signup-email"
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange}
                  autoComplete="email"
                  placeholder="Enter your email"
                  disabled={isSubmitting}
                />
              </div>

              <div className="create-account-group">
                <label htmlFor="signup-password">
                  Password
                </label>

                <div className="create-password-wrapper">
                  <input
                    id="signup-password"
                    name="password"
                    type={
                      showPassword
                        ? "text"
                        : "password"
                    }
                    value={formData.password}
                    onChange={handleChange}
                    autoComplete="new-password"
                    placeholder="Create a password"
                    disabled={isSubmitting}
                  />

                  <button
                    type="button"
                    className="create-password-toggle"
                    onClick={() =>
                      setShowPassword(
                        (current) => !current,
                      )
                    }
                    aria-label={
                      showPassword
                        ? "Hide password"
                        : "Show password"
                    }
                    disabled={isSubmitting}
                  >
                    {showPassword
                      ? "Hide"
                      : "Show"}
                  </button>
                </div>
              </div>

              <div className="create-account-group">
                <label htmlFor="confirm-password">
                  Confirm password
                </label>

                <div className="create-password-wrapper">
                  <input
                    id="confirm-password"
                    name="confirmPassword"
                    type={
                      showConfirmPassword
                        ? "text"
                        : "password"
                    }
                    value={
                      formData.confirmPassword
                    }
                    onChange={handleChange}
                    autoComplete="new-password"
                    placeholder="Repeat your password"
                    disabled={isSubmitting}
                  />

                  <button
                    type="button"
                    className="create-password-toggle"
                    onClick={() =>
                      setShowConfirmPassword(
                        (current) => !current,
                      )
                    }
                    aria-label={
                      showConfirmPassword
                        ? "Hide confirmed password"
                        : "Show confirmed password"
                    }
                    disabled={isSubmitting}
                  >
                    {showConfirmPassword
                      ? "Hide"
                      : "Show"}
                  </button>
                </div>
              </div>

              {/*
              <div className="create-account-recaptcha">
                <ReCAPTCHA
                  ref={recaptchaRef}
                  sitekey={
                    import.meta.env
                      .VITE_RECAPTCHA_SITE_KEY
                  }
                  onChange={(token) => {
                    setRecaptchaToken(
                      token || "",
                    );
                    setErrorMessage("");
                  }}
                  onExpired={() =>
                    setRecaptchaToken("")
                  }
                  onErrored={() => {
                    setRecaptchaToken("");
                    setErrorMessage(
                      "reCAPTCHA could not be loaded. Please try again.",
                    );
                  }}
                />
              </div>
              */}

              {errorMessage && (
                <p
                  className="create-account-error"
                  role="alert"
                >
                  {errorMessage}
                </p>
              )}

              <button
                type="submit"
                className="create-account-button"
                disabled={isSubmitting}
              >
                {isSubmitting
                  ? "Creating..."
                  : "Sign up"}
              </button>
            </form>

            <p className="create-account-login-text">
              Already have an account?{" "}
              <Link href="/login">
                Login
              </Link>
            </p>
          </div>
        </div>
      </section>
    </main>
  );
}

export default CreateAccount;