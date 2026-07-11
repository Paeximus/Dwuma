// =============================================
// skills-gap.js  —  AI Career Agent
// Handles the Skills Gap Analysis module
// =============================================

const API_BASE = '/api';

// --- Tag / Skill Input ---
const skillTags = [];
const skillInput = document.getElementById('skillInput');
const skillsWrapper = document.getElementById('skillsWrapper');

skillInput.addEventListener('keydown', (e) => {
  if (e.key === 'Enter' || e.key === ',') {
    e.preventDefault();
    addTag(skillInput.value.trim().replace(/,$/, ''));
  }
  if (e.key === 'Backspace' && skillInput.value === '' && skillTags.length > 0) {
    removeTag(skillTags.length - 1);
  }
});

skillsWrapper.addEventListener('click', () => skillInput.focus());

function addTag(value) {
  if (!value || skillTags.includes(value.toLowerCase())) return;
  skillTags.push(value.toLowerCase());
  renderTags();
  skillInput.value = '';
}

function removeTag(index) {
  skillTags.splice(index, 1);
  renderTags();
}

function renderTags() {
  // Remove existing tags
  skillsWrapper.querySelectorAll('.tag').forEach(t => t.remove());
  skillTags.forEach((tag, i) => {
    const el = document.createElement('div');
    el.className = 'tag';
    el.innerHTML = `${tag} <button onclick="removeTag(${i})" title="Remove">×</button>`;
    skillsWrapper.insertBefore(el, skillInput);
  });
}

// --- Loading messages cycle ---
const loadingMessages = [
  'Analysing your skills profile...',
  'Comparing against role requirements...',
  'Identifying skill gaps...',
  'Generating learning recommendations...',
];

let loadingInterval;
function startLoadingCycle() {
  let i = 0;
  document.getElementById('loadingText').textContent = loadingMessages[0];
  loadingInterval = setInterval(() => {
    i = (i + 1) % loadingMessages.length;
    document.getElementById('loadingText').textContent = loadingMessages[i];
  }, 2000);
}

function stopLoadingCycle() {
  clearInterval(loadingInterval);
}

// --- Form Validation ---
function validateForm() {
  if (skillTags.length === 0) return 'Please add at least one skill.';
  if (!document.getElementById('jobTitle').value.trim()) return 'Please enter a target job title.';
  if (!document.getElementById('industry').value) return 'Please select an industry.';
  return null;
}

// --- Main Analysis ---
async function runAnalysis() {
  const error = validateForm();
  const errorEl = document.getElementById('formError');

  if (error) {
    errorEl.textContent = error;
    errorEl.style.display = 'block';
    return;
  }

  errorEl.style.display = 'none';

  const payload = {
    skills: skillTags,
    education: document.getElementById('education').value,
    fieldOfStudy: document.getElementById('fieldOfStudy').value,
    experience: document.getElementById('experience').value,
    jobTitle: document.getElementById('jobTitle').value.trim(),
    industry: document.getElementById('industry').value,
    jobDescription: document.getElementById('jobDescription').value.trim(),
  };

  // Show loading, hide input
  document.getElementById('inputPanel').style.display = 'none';
  document.getElementById('loadingPanel').style.display = 'block';
  startLoadingCycle();

  try {
    const result = await callSkillsGapAPI(payload);
    stopLoadingCycle();
    document.getElementById('loadingPanel').style.display = 'none';
    renderResults(result, payload.jobTitle);
  } catch (err) {
    stopLoadingCycle();
    document.getElementById('loadingPanel').style.display = 'none';
    document.getElementById('inputPanel').style.display = 'block';
    errorEl.textContent = 'Analysis failed: ' + (err.message || 'Server error. Please try again.');
    errorEl.style.display = 'block';
  }
}

// --- API Call ---
async function callSkillsGapAPI(payload) {
  const response = await fetch(`${API_BASE}/skillsgap/analyse`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      // 'Authorization': `Bearer ${getToken()}` // Uncomment when auth is ready
    },
    body: JSON.stringify(payload),
  });

  if (!response.ok) {
    const err = await response.json().catch(() => ({}));
    throw new Error(err.message || `HTTP ${response.status}`);
  }

  return response.json();
}

// --- Render Results ---
function renderResults(data, jobTitle) {
  document.getElementById('resultsPanel').style.display = 'block';
  document.getElementById('roleNameDisplay').textContent = jobTitle;

  // Stats
  renderStats(data);

  // Overall match bar
  const pct = data.matchPercentage ?? 0;
  document.getElementById('matchPct').textContent = `${pct}%`;
  setTimeout(() => {
    document.getElementById('matchBar').style.width = `${pct}%`;
  }, 100);

  document.getElementById('overallSummary').textContent = data.summary ?? '';

  // Skill breakdown
  renderSkillBreakdown(data.skills ?? []);

  // Resources
  renderResources(data.resources ?? []);
}

function renderStats(data) {
  const skills = data.skills ?? [];
  const present = skills.filter(s => s.status === 'present').length;
  const partial  = skills.filter(s => s.status === 'partial').length;
  const absent   = skills.filter(s => s.status === 'absent').length;

  document.getElementById('statsRow').innerHTML = `
    <div class="stat-box">
      <div class="stat-number stat-green">${present}</div>
      <div class="stat-label">Skills You Have</div>
    </div>
    <div class="stat-box">
      <div class="stat-number stat-warn">${partial}</div>
      <div class="stat-label">Partially Developed</div>
    </div>
    <div class="stat-box">
      <div class="stat-number stat-red">${absent}</div>
      <div class="stat-label">Skills to Learn</div>
    </div>
  `;
}

function renderSkillBreakdown(skills) {
  const groups = { present: [], partial: [], absent: [] };
  skills.forEach(s => {
    if (groups[s.status]) groups[s.status].push(s);
  });

  const labels = {
    present: '✅ Skills You Already Have',
    partial:  '⚡ Partially Developed',
    absent:   '❌ Skills to Acquire',
  };

  let html = '';
  for (const [status, items] of Object.entries(groups)) {
    if (items.length === 0) continue;
    html += `<div class="gap-section">
      <div class="gap-section-title ${status}">${labels[status]}</div>`;
    items.forEach(skill => {
      html += `
        <div class="skill-item">
          <div class="skill-badge badge-${status}"></div>
          <div class="skill-info">
            <div class="skill-name">${escHtml(skill.name)}</div>
            <div class="skill-level">${escHtml(skill.level ?? '')}</div>
            <div class="skill-desc">${escHtml(skill.description ?? '')}</div>
          </div>
        </div>`;
    });
    html += '</div>';
  }

  document.getElementById('skillBreakdown').innerHTML = html || '<p class="empty-state">No skill data returned.</p>';
}

function renderResources(resources) {
  if (!resources.length) {
    document.getElementById('resourceList').innerHTML =
      '<div class="empty-state"><div class="empty-icon">📭</div><p>No resources returned.</p></div>';
    return;
  }

  const html = resources.map(r => `
    <div class="resource-card" style="margin-bottom:12px;">
      <div class="resource-platform">${escHtml(r.platform ?? '')}</div>
      <div class="resource-name">${escHtml(r.name)}</div>
      <div class="resource-desc">${escHtml(r.description ?? '')}</div>
      ${r.url ? `<a href="${escHtml(r.url)}" target="_blank" rel="noopener" class="resource-link">Open resource →</a>` : ''}
    </div>`).join('');

  document.getElementById('resourceList').innerHTML = html;
}

// --- Helpers ---
function escHtml(str) {
  return String(str)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function clearForm() {
  skillTags.length = 0;
  renderTags();
  ['education','industry','experience'].forEach(id => document.getElementById(id).value = '');
  ['fieldOfStudy','jobTitle','jobDescription'].forEach(id => document.getElementById(id).value = '');
  document.getElementById('formError').style.display = 'none';
}

function resetAnalysis() {
  document.getElementById('resultsPanel').style.display = 'none';
  document.getElementById('inputPanel').style.display = 'block';
}

function saveReport() {
  // In production: POST to /api/reports/save
  alert('Report saved! (Connect to backend to persist this data.)');
}

function getToken() {
  return localStorage.getItem('jwt_token') ?? '';
}
