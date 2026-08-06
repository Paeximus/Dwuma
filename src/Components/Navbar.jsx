import { Link } from "wouter";
import logo from "../assets/logo.svg";

function Navbar() {
  return (
    <header className="navbar">
      <div className="navbar-logo">
        <Link href="/">
          <img src={logo} alt="Dwuma Logo" />
        </Link>
      </div>

      <nav className="navbar-links">
        <a href="#jobs">Jobs</a>
        <a href="#cv-tailor">CV Tailor</a>
        <a href="#interview-coach">Interview Coach</a>

        <Link href="/login" className="signup-btn">
          Login
        </Link>
      </nav>
    </header>
  );
}

export default Navbar;
