import "./App.css";

import { Route, Switch } from "wouter";

import Navbar from "./Components/Navbar";
import Hero from "./Components/Hero";
import SectionTwo from "./Components/SectionTwo";
import SectionThree from "./Components/SectionThree";
import SectionFour from "./Components/SectionFour";

import Login from "./pages/Login";
import CreateAccount from "./pages/CreateAccount";
import ForgotPassword from "./pages/ForgotPassword";
import EmailVerification from "./pages/EmailVerification";
import Dashboard from "./pages/Dashboard";
import CareerPreferences from "./pages/CareerPreferences";
import OnboardingStepTwo from "./pages/OnboardingStepTwo";
import InterviewCoach from "./pages/InterviewCoach";
import CvTailor from "./pages/CvTailor";
import CvTailorResults from "./pages/CvTailorResults";
import Profile from "./pages/Profile";
import Notifications from "./pages/Notifications";
import Settings from "./pages/Settings";
import Jobs from "./pages/Jobs";

function LandingPage() {
  return (
    <>
      <Navbar />

      <main>
        <Hero />
        <SectionTwo />
        <SectionThree />
        <SectionFour />
      </main>
    </>
  );
}

function NotFound() {
  return (
    <main className="not-found-page">
      <h1>Page not found</h1>
      <p>The page you are looking for does not exist.</p>
    </main>
  );
}

function App() {
  return (
    <Switch>
      {/* Landing page */}
      <Route path="/" component={LandingPage} />

      {/* Authentication */}
      <Route path="/login" component={Login} />

      <Route
        path="/create-account"
        component={CreateAccount}
      />

      <Route
        path="/forgot-password"
        component={ForgotPassword}
      />

      <Route
        path="/email-verification"
        component={EmailVerification}
      />

      {/* Onboarding */}
      <Route
        path="/onboarding/step-1"
        component={CareerPreferences}
      />

      <Route
        path="/onboarding/step-2"
        component={OnboardingStepTwo}
      />

      {/* Dashboard pages */}
      <Route
        path="/dashboard/interview-coach"
        component={InterviewCoach}
      />

      <Route
        path="/dashboard/cv-tailor"
        component={CvTailor}
      />

      <Route
        path="/dashboard/cv-tailor/results"
        component={CvTailorResults}
      />

      <Route
        path="/dashboard/jobs"
        component={Jobs}
      />

      <Route
        path="/dashboard"
        component={Dashboard}
      />

      <Route
        path="/dashboard/profile"
        component={Profile}
      />

      <Route
        path="/dashboard/notifications"
        component={Notifications}
      />

      <Route
        path="/dashboard/settings"
        component={Settings}
      />

      {/* Page not found */}
      <Route component={NotFound} />
    </Switch>
  );
}

export default App;
