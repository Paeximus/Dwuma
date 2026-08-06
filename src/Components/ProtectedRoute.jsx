import { useEffect } from "react";
import { useLocation } from "wouter";

function ProtectedRoute({ children }) {
  const [, navigate] = useLocation();

  const token =
    localStorage.getItem("dwumaToken") ||
    sessionStorage.getItem("dwumaToken");

  useEffect(() => {
    if (!token) {
      navigate("/login", { replace: true });
    }
  }, [token, navigate]);

  if (!token) {
    return null;
  }

  return children;
}

export default ProtectedRoute;