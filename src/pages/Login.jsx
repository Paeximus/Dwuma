import logo from "../assets/logo.svg";
import { useEffect, useState } from "react";
import { Link, useLocation } from "wouter";
import loginImage from "../assets/login.svg";
import "./Login.css";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

function Login() {
  const [, navigate] = useLocation();

  const [formData, setFormData] = useState({
    username: "",
    password: "",
  });

  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    const token =
      localStorage.getItem("dwumaToken") ||
      sessionStorage.getItem("dwumaToken");

    if (token) {
      navigate("/dashboard", {
        replace: true,
      });
    }
  }, [navigate]);

  const handleInputChange = (event) => {
    const { name, value } = event.target;

    setFormData((previousData) => ({
      ...previousData,
      [name]: value,
    }));

    setErrorMessage("");
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setErrorMessage("");

    const username = formData.username.trim();
    const password = formData.password;

    if (!username || !password) {
      setErrorMessage("Please enter your username and password.");
      return;
    }

    if (!API_BASE_URL) {
      setErrorMessage(
        "The backend API address has not been configured.",
      );
      return;
    }

    setIsSubmitting(true);

    try {
      const response = await fetch(
        `${API_BASE_URL}/auth/login`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
          },
          body: JSON.stringify({
            usernameOrEmail: username,
            password,
          }),
        },
      );

      const data = await response.json().catch(() => null);

      if (!response.ok) {
        if (
          response.status === 403 &&
          data?.requiresEmailVerification
        ) {
          sessionStorage.setItem(
            "pendingVerificationEmail",
            data.email || username,
          );

          navigate("/email-verification", {
            replace: true,
          });

          return;
        }

        throw new Error(
          data?.message ||
            data?.title ||
            "The username or password is incorrect.",
        );
      }

      const token =
        data?.token ||
        data?.accessToken ||
        data?.jwtToken;

      if (!token) {
        throw new Error(
          "The login response did not contain an authentication token.",
        );
      }

      const user = data?.user || {
        username: data?.username || username,
        email: data?.email || "",
        firstName: data?.firstName || "",
      };

      if (rememberMe) {
        localStorage.setItem("dwumaToken", token);
        localStorage.setItem(
          "dwumaUser",
          JSON.stringify(user),
        );

        sessionStorage.removeItem("dwumaToken");
        sessionStorage.removeItem("dwumaUser");
      } else {
        sessionStorage.setItem("dwumaToken", token);
        sessionStorage.setItem(
          "dwumaUser",
          JSON.stringify(user),
        );

        localStorage.removeItem("dwumaToken");
        localStorage.removeItem("dwumaUser");
      }

      sessionStorage.removeItem(
        "pendingVerificationEmail",
      );

      navigate("/dashboard", {
        replace: true,
      });
    } catch (error) {
      console.error("Login error:", error);

      setErrorMessage(
        error instanceof Error
          ? error.message
          : "Login failed. Please try again.",
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <main className="login-page">
      <section className="login-container">
        <div className="login-illustration">
          <div className="login-brand">
            <img
              src={logo}
              alt="Dwuma Logo"
              className="login-logo"
            />
          </div>

          <img
            src={loginImage}
            alt="Woman using a laptop"
            className="login-image"
          />
        </div>

        <div className="login-panel">
          <div className="login-form-wrapper">
            <header className="login-header">
              <h1>Login</h1>
              <p>Gain insights and answers.</p>
            </header>

            <form
              className="login-form"
              onSubmit={handleSubmit}
            >
              <div className="form-group">
                <label htmlFor="username">
                  Username or email
                </label>

                <input
                  id="username"
                  name="username"
                  type="text"
                  value={formData.username}
                  onChange={handleInputChange}
                  autoComplete="username"
                  placeholder="Enter your username"
                  disabled={isSubmitting}
                />
              </div>

              <div className="form-group">
                <label htmlFor="password">
                  Password
                </label>

                <div className="password-input-wrapper">
                  <input
                    id="password"
                    name="password"
                    type={
                      showPassword
                        ? "text"
                        : "password"
                    }
                    value={formData.password}
                    onChange={handleInputChange}
                    autoComplete="current-password"
                    placeholder="Enter your password"
                    disabled={isSubmitting}
                  />

                  <button
                    type="button"
                    className="password-toggle"
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
                  >
                    {showPassword ? (
                      <svg
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                        className="eye-icon"
                      >
                        <path d="M3.3 2 22 20.7l-1.3 1.3-3.2-3.2A11.8 11.8 0 0 1 12 20C5 20 1 12 1 12a18.2 18.2 0 0 1 4.2-5.2L2 3.3 3.3 2Zm3.3 6.2A14.8 14.8 0 0 0 3.2 12c1.2 2 4.3 6 8.8 6a9.6 9.6 0 0 0 4-.8l-2-2a4.5 4.5 0 0 1-5.2-5.2L6.6 8.2ZM12 4c7 0 11 8 11 8a18 18 0 0 1-3.2 4.3l-1.4-1.4a14.4 14.4 0 0 0 2.4-2.9C19.6 10 16.5 6 12 6c-.8 0-1.5.1-2.2.3L8.2 4.7C9.4 4.2 10.7 4 12 4Zm-.2 3.5h.2a4.5 4.5 0 0 1 4.5 4.5v.2l-4.7-4.7Z" />
                      </svg>
                    ) : (
                      <svg
                        viewBox="0 0 24 24"
                        aria-hidden="true"
                        className="eye-icon"
                      >
                        <path d="M12 4c7 0 11 8 11 8s-4 8-11 8S1 12 1 12s4-8 11-8Zm0 2c-4.5 0-7.6 4-8.8 6 1.2 2 4.3 6 8.8 6s7.6-4 8.8-6C19.6 10 16.5 6 12 6Zm0 2.5a3.5 3.5 0 1 1 0 7 3.5 3.5 0 0 1 0-7Zm0 2a1.5 1.5 0 1 0 0 3 1.5 1.5 0 0 0 0-3Z" />
                      </svg>
                    )}
                  </button>
                </div>
              </div>

              <div className="login-options">
                <label className="remember-option">
                  <input
                    type="checkbox"
                    checked={rememberMe}
                    onChange={(event) =>
                      setRememberMe(
                        event.target.checked,
                      )
                    }
                    disabled={isSubmitting}
                  />

                  <span className="custom-checkbox" />

                  <span>Remember me</span>
                </label>

                <Link
                  href="/forgot-password"
                  className="forgot-password-link"
                >
                  Forgot Password?
                </Link>
              </div>

              {errorMessage && (
                <p
                  className="login-error"
                  role="alert"
                >
                  {errorMessage}
                </p>
              )}

              <button
                type="submit"
                className="login-button"
                disabled={isSubmitting}
              >
                {isSubmitting
                  ? "Logging in..."
                  : "Login"}
              </button>
            </form>

            <Link
              href="/create-account"
              className="register-link"
            >
              Register an account
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}

export default Login;