import { useEffect, useRef, useState } from "react";
import { useLocation } from "wouter";
import {
  Bell,
  BriefcaseBusiness,
  FileText,
  LayoutDashboard,
  Menu,
  Mic,
  RotateCcw,
  Search,
  Send,
  Settings,
  UserRound,
  UsersRound,
} from "lucide-react";

import logo from "../assets/logo.svg";
import "./InterviewCoach.css";

const MAX_ANSWER_LENGTH = 2000;
const MIN_ANSWER_WORDS = 3;

const ALLOWED_INTERVIEW_ROLES = [
  "Software Developer",
  "Frontend Developer",
  "Backend Developer",
  "Full Stack Developer",
  "Mobile App Developer",
  "Data Analyst",
  "Data Scientist",
  "Cybersecurity Analyst",
  "Cloud Engineer",
  "Network Engineer",
  "UI/UX Designer",
  "Product Manager",
  "Project Manager",
  "Business Analyst",
  "Digital Marketer",
  "Graphic Designer",
  "Accountant",
  "Financial Analyst",
  "Human Resource Officer",
  "Customer Service Representative",
  "Sales Representative",
  "Administrative Assistant",
  "Civil Engineer",
  "Electrical Engineer",
  "Mechanical Engineer",
  "Graduate Trainee",
];

const BLOCKED_CODE_PATTERNS = [
  /<\s*script\b/i,
  /<\s*iframe\b/i,
  /<\s*object\b/i,
  /<\s*embed\b/i,
  /<\s*svg\b/i,
  /javascript\s*:/i,
  /vbscript\s*:/i,
  /data\s*:\s*text\/html/i,
  /\bon\w+\s*=/i,
  /\beval\s*\(/i,
  /\bnew\s+Function\s*\(/i,
  /\bdocument\s*\.\s*(cookie|write)/i,
  /\bwindow\s*\.\s*location/i,
];

const BLOCKED_PROMPT_PATTERNS = [
  /ignore\s+(all\s+)?(previous|prior|above)\s+instructions/i,
  /disregard\s+(all\s+)?(previous|prior|above)\s+instructions/i,
  /override\s+(the\s+)?(system|developer)\s+instructions/i,
  /reveal\s+(your\s+)?(system|hidden|developer)\s+prompt/i,
  /show\s+(me\s+)?(your\s+)?(system|hidden|developer)\s+instructions/i,
  /print\s+(your\s+)?(system|hidden|developer)\s+prompt/i,
  /act\s+as\s+(the\s+)?system/i,
];

function createMessage(sender, text) {
  return {
    id:
      typeof crypto !== "undefined" &&
      typeof crypto.randomUUID === "function"
        ? crypto.randomUUID()
        : `${Date.now()}-${Math.random()}`,
    sender,
    text,
  };
}

function normaliseInput(value) {
  return value
    .normalize("NFKC")
    .replace(/\u0000/g, "")
    .replace(
      /[\u0001-\u0008\u000B\u000C\u000E-\u001F\u007F]/g,
      ""
    )
    .replace(/\r\n?/g, "\n")
    .replace(/[ \t]+/g, " ")
    .trim();
}

function containsBlockedCode(value) {
  return BLOCKED_CODE_PATTERNS.some((pattern) =>
    pattern.test(value)
  );
}

function containsPromptInjection(value) {
  return BLOCKED_PROMPT_PATTERNS.some((pattern) =>
    pattern.test(value)
  );
}

function containsUrl(value) {
  return /(https?:\/\/|www\.|[a-z0-9-]+\.(com|net|org|io|dev|xyz)\b)/i.test(
    value
  );
}

function isNumbersOnly(value) {
  return /^[\d\s.,+\-*/=()]+$/.test(value);
}

function hasEnoughLetters(value) {
  const letters = value.match(/[a-z]/gi) || [];
  const visibleCharacters = value.replace(/\s/g, "");

  if (!visibleCharacters.length) {
    return false;
  }

  return letters.length / visibleCharacters.length >= 0.5;
}

function hasRepeatedCharacters(value) {
  return /(.)\1{5,}/i.test(value);
}

function hasRepeatedWords(value) {
  const words = value
    .toLowerCase()
    .split(/\s+/)
    .filter(Boolean);

  if (words.length < 4) {
    return false;
  }

  const uniqueWords = new Set(words);

  return uniqueWords.size / words.length < 0.35;
}

function looksLikeGibberish(value) {
  const words = value
    .toLowerCase()
    .split(/\s+/)
    .filter(Boolean);

  if (!words.length) {
    return true;
  }

  const suspiciousWords = words.filter((word) => {
    const lettersOnly = word.replace(/[^a-z]/gi, "");

    if (lettersOnly.length < 5) {
      return false;
    }

    const hasVowel = /[aeiou]/i.test(lettersOnly);
    const longConsonantSequence =
      /[bcdfghjklmnpqrstvwxyz]{6,}/i.test(
        lettersOnly
      );

    return !hasVowel || longConsonantSequence;
  });

  return suspiciousWords.length / words.length > 0.5;
}

function validateRole(role) {
  const cleanedRole = normaliseInput(role);

  if (!cleanedRole) {
    return {
      isValid: false,
      error: "Please select an interview role.",
    };
  }

  if (!ALLOWED_INTERVIEW_ROLES.includes(cleanedRole)) {
    return {
      isValid: false,
      error: "Please select a role from the available list.",
    };
  }

  return {
    isValid: true,
    value: cleanedRole,
    error: "",
  };
}

function validateInterviewAnswer(value) {
  const cleanedValue = normaliseInput(value);

  if (!cleanedValue) {
    return {
      isValid: false,
      error: "Please enter an interview answer.",
    };
  }

  if (cleanedValue.length > MAX_ANSWER_LENGTH) {
    return {
      isValid: false,
      error: `Your answer must not exceed ${MAX_ANSWER_LENGTH} characters.`,
    };
  }

  if (containsBlockedCode(cleanedValue)) {
    return {
      isValid: false,
      error:
        "Code, scripts and executable markup are not accepted.",
    };
  }

  if (containsPromptInjection(cleanedValue)) {
    return {
      isValid: false,
      error:
        "System instructions and prompt override requests are not accepted.",
    };
  }

  if (containsUrl(cleanedValue)) {
    return {
      isValid: false,
      error: "Links are not accepted in interview answers.",
    };
  }

  if (isNumbersOnly(cleanedValue)) {
    return {
      isValid: false,
      error: "Your answer must contain meaningful words.",
    };
  }

  if (!hasEnoughLetters(cleanedValue)) {
    return {
      isValid: false,
      error: "Please enter a meaningful written answer.",
    };
  }

  if (
    hasRepeatedCharacters(cleanedValue) ||
    hasRepeatedWords(cleanedValue) ||
    looksLikeGibberish(cleanedValue)
  ) {
    return {
      isValid: false,
      error:
        "Your answer appears to contain random or repeated text.",
    };
  }

  const words = cleanedValue
    .split(/\s+/)
    .filter(Boolean);

  if (words.length < MIN_ANSWER_WORDS) {
    return {
      isValid: false,
      error: `Please provide at least ${MIN_ANSWER_WORDS} words.`,
    };
  }

  return {
    isValid: true,
    value: cleanedValue,
    error: "",
  };
}

function InterviewCoach() {
  const [, navigate] = useLocation();
  const chatEndRef = useRef(null);

  const [selectedRole, setSelectedRole] =
    useState("");
  const [message, setMessage] = useState("");
  const [inputError, setInputError] = useState("");
  const [menuOpen, setMenuOpen] = useState(false);
  const [interviewStarted, setInterviewStarted] =
    useState(false);
  const [isCoachTyping, setIsCoachTyping] =
    useState(false);
  const [conversation, setConversation] = useState([]);

  useEffect(() => {
    if (!interviewStarted) {
      return;
    }

    chatEndRef.current?.scrollIntoView({
      behavior: "smooth",
      block: "end",
    });
  }, [conversation, isCoachTyping, interviewStarted]);

  function closeMenu() {
    setMenuOpen(false);
  }

  function goToPage(path) {
    closeMenu();
    navigate(path);
  }

  function handleRoleChange(event) {
    setSelectedRole(event.target.value);
    setInputError("");
  }

  function handleMessageChange(event) {
    setMessage(event.target.value);
    setInputError("");
  }

  function handleSetupSubmit(event) {
    event.preventDefault();

    const validation = validateRole(selectedRole);

    if (!validation.isValid) {
      setInputError(validation.error);
      return;
    }

    const role = validation.value;

    setInputError("");
    setMessage("");
    setInterviewStarted(true);

    setConversation([
      createMessage(
        "coach",
        `Welcome to your ${role} interview practice session. Please introduce yourself and explain why you are interested in this role.`
      ),
    ]);

    console.log("Interview started:", {
      role,
    });
  }

  function handleChatSubmit(event) {
    event.preventDefault();

    if (isCoachTyping) {
      return;
    }

    const validation =
      validateInterviewAnswer(message);

    if (!validation.isValid) {
      setInputError(validation.error);
      return;
    }

    const cleanedAnswer = validation.value;

    setConversation((currentConversation) => [
      ...currentConversation,
      createMessage("user", cleanedAnswer),
    ]);

    setMessage("");
    setInputError("");
    setIsCoachTyping(true);

    console.log("Interview answer:", {
      role: selectedRole,
      answer: cleanedAnswer,
    });

    /*
      Replace the temporary response below with:

      POST /api/interview-coach/message

      The ASP.NET backend must perform the same validation.
    */

    window.setTimeout(() => {
      setConversation((currentConversation) => [
        ...currentConversation,
        createMessage(
          "coach",
          "Thank you. Describe a challenging project or task you completed, the difficulty you faced, and how you handled it."
        ),
      ]);

      setIsCoachTyping(false);
    }, 900);
  }

  function handleMicrophoneClick() {
    setInputError(
      "Voice input will become available when the audio API is connected."
    );
  }

  function startNewInterview() {
    setSelectedRole("");
    setConversation([]);
    setMessage("");
    setInputError("");
    setIsCoachTyping(false);
    setInterviewStarted(false);
    closeMenu();
  }

  function renderNavigationMenu() {
    if (!menuOpen) {
      return null;
    }

    return (
      <>
        <button
          type="button"
          className="interview-menu-overlay"
          onClick={closeMenu}
          aria-label="Close navigation menu"
        />

        <aside
          className="interview-menu"
          aria-label="Dashboard navigation"
        >
          <button
            type="button"
            onClick={() => goToPage("/dashboard")}
          >
            <LayoutDashboard size={17} />
            <span>Dashboard</span>
          </button>

          <button
            type="button"
            className="interview-menu-active"
            onClick={closeMenu}
            aria-current="page"
          >
            <UsersRound size={17} />
            <span>Interview Coach</span>
          </button>

          <button
            type="button"
            onClick={() =>
              goToPage("/dashboard/cv-tailor")
            }
          >
            <FileText size={17} />
            <span>CV Tailor</span>
          </button>

          <button
            type="button"
            onClick={() =>
              goToPage("/dashboard/jobs")
            }
          >
            <BriefcaseBusiness size={17} />
            <span>Jobs</span>
          </button>

          <button
            type="button"
            onClick={() =>
              goToPage("/dashboard/profile")
            }
          >
            <UserRound size={17} />
            <span>Profile</span>
          </button>

          <button
            type="button"
            onClick={() =>
              goToPage("/dashboard/notifications")
            }
          >
            <Bell size={17} />
            <span>Notifications</span>
          </button>

          <button
            type="button"
            onClick={() =>
              goToPage("/dashboard/settings")
            }
          >
            <Settings size={17} />
            <span>Settings</span>
          </button>
        </aside>
      </>
    );
  }

  function renderInputError() {
    if (!inputError) {
      return null;
    }

    return (
      <p
        id="interview-input-error"
        className="interview-input-error"
        role="alert"
      >
        {inputError}
      </p>
    );
  }

  function renderSetupScreen() {
    return (
      <div className="interview-content">
        <div className="interview-heading">
          <h1>Interview Ready?</h1>

          <p>
            Practice with real-world interview questions
            and candidate experiences
          </p>
        </div>

        <form
          className="interview-search-form"
          onSubmit={handleSetupSubmit}
          noValidate
        >
          <select
            value={selectedRole}
            onChange={handleRoleChange}
            aria-label="Select an interview role"
            aria-invalid={Boolean(inputError)}
            aria-describedby={
              inputError
                ? "interview-input-error"
                : undefined
            }
          >
            <option value="">
              Select an interview role
            </option>

            {ALLOWED_INTERVIEW_ROLES.map((role) => (
              <option key={role} value={role}>
                {role}
              </option>
            ))}
          </select>

          <div className="interview-input-actions">
            <button
              type="button"
              className="interview-icon-button"
              onClick={handleMicrophoneClick}
              aria-label="Use microphone"
              title="Use microphone"
            >
              <Mic size={19} strokeWidth={2.2} />
            </button>

            <button
              type="submit"
              className="interview-icon-button"
              disabled={!selectedRole}
              aria-label="Start interview"
            >
              <Search size={20} strokeWidth={2.3} />
            </button>
          </div>
        </form>

        {renderInputError()}

        <p className="interview-example-text">
          Select a role to begin your interview practice
        </p>
      </div>
    );
  }

  function renderChatScreen() {
    return (
      <div className="interview-chat-layout">
        <div className="interview-chat-header">
          <div>
            <h1>Interview Session</h1>

            <p>
              {selectedRole} interview practice
            </p>
          </div>

          <button
            type="button"
            className="interview-new-button"
            onClick={startNewInterview}
          >
            <RotateCcw size={15} />
            <span>New Interview</span>
          </button>
        </div>

        <section
          className="interview-chat-area"
          aria-label="Interview conversation"
          aria-live="polite"
        >
          {conversation.map((chatMessage) => (
            <div
              key={chatMessage.id}
              className={`interview-message-row ${
                chatMessage.sender === "user"
                  ? "interview-message-row-user"
                  : "interview-message-row-coach"
              }`}
            >
              <div
                className={`interview-message ${
                  chatMessage.sender === "user"
                    ? "interview-message-user"
                    : "interview-message-coach"
                }`}
              >
                <span className="interview-message-name">
                  {chatMessage.sender === "user"
                    ? "You"
                    : "Interview Coach"}
                </span>

                <p>{chatMessage.text}</p>
              </div>
            </div>
          ))}

          {isCoachTyping && (
            <div className="interview-message-row interview-message-row-coach">
              <div className="interview-message interview-message-coach interview-typing-message">
                <span className="interview-message-name">
                  Interview Coach
                </span>

                <div
                  className="interview-typing-dots"
                  aria-label="Interview Coach is preparing a response"
                >
                  <span />
                  <span />
                  <span />
                </div>
              </div>
            </div>
          )}

          <div ref={chatEndRef} />
        </section>

        <div className="interview-chat-input-section">
          <form
            className="interview-chat-form"
            onSubmit={handleChatSubmit}
            noValidate
          >
            <input
              type="text"
              value={message}
              onChange={handleMessageChange}
              maxLength={MAX_ANSWER_LENGTH}
              placeholder="Type your answer..."
              aria-label="Type your interview answer"
              aria-invalid={Boolean(inputError)}
              aria-describedby={
                inputError
                  ? "interview-input-error"
                  : undefined
              }
              autoComplete="off"
              spellCheck="true"
            />

            <div className="interview-input-actions">
              <button
                type="button"
                className="interview-icon-button"
                onClick={handleMicrophoneClick}
                aria-label="Answer with microphone"
                title="Answer with microphone"
              >
                <Mic size={19} strokeWidth={2.2} />
              </button>

              <button
                type="submit"
                className="interview-send-button"
                disabled={
                  !message.trim() || isCoachTyping
                }
                aria-label="Send answer"
              >
                <Send size={18} strokeWidth={2.3} />
              </button>
            </div>
          </form>

          <div className="interview-chat-form-footer">
            {renderInputError()}

            <span className="interview-character-count">
              {message.length}/{MAX_ANSWER_LENGTH}
            </span>
          </div>
        </div>
      </div>
    );
  }

  return (
    <main
      className={`interview-screen ${
        interviewStarted
          ? "interview-screen-active"
          : ""
      }`}
    >
      <header className="interview-header">
        <button
          type="button"
          className="interview-logo-button"
          onClick={() => navigate("/dashboard")}
          aria-label="Go to dashboard"
        >
          <img
            src={logo}
            alt="DWUMA"
            className="interview-logo"
          />
        </button>
      </header>

      <section
        className={`interview-body ${
          interviewStarted
            ? "interview-body-active"
            : ""
        }`}
      >
        <button
          type="button"
          className="interview-menu-button"
          onClick={() =>
            setMenuOpen((currentValue) => !currentValue)
          }
          aria-label={
            menuOpen
              ? "Close navigation menu"
              : "Open navigation menu"
          }
          aria-expanded={menuOpen}
        >
          <Menu size={29} strokeWidth={1.8} />
        </button>

        {renderNavigationMenu()}

        {interviewStarted
          ? renderChatScreen()
          : renderSetupScreen()}
      </section>
    </main>
  );
}

export default InterviewCoach;