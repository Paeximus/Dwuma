// =============================================
// profile.js  —  AI Career Agent
// Loads and renders the user's saved profile
// =============================================

const API_BASE = 'http://localhost:5000/api';

document.addEventListener('DOMContentLoaded', loadProfile);

async function loadProfile() {
  // Try backend first; fall back to localStorage during dev
  let profile = null;

  const userId = localStorage.getItem('userId');
  if (userId) {
    try {
      const res = await fetch(`${API_BASE}/profile/${userId}`);
      if (res.ok) profile = await res.json();
    } catch (_) { /* offline — fall through */ }
  }

  if (!profile) {
    const local = localStorage.getItem('userProfile');
    profile = local ? JSON.parse(local) : null;
  }

  if (!profile) {
    window.location.href = 'onboarding.html';
    return;
  }

  renderProfile(profile);
}

function renderProfile(p) {
  // Avatar & header
  const initials = `${(p.firstName || '?')[0]}${(p.lastName || '?')[0]}`.toUpperCase();
  document.getElementById('avatarInitials').textContent = initials;
  document.getElementById('profileFullName').textContent = `${p.firstName} ${p.lastName}`;
  document.getElementById('profileSubtitle').textContent =
    p.fieldOfStudy ? `${p.degreeLevel} · ${p.fieldOfStudy}` : '';

  // Completion score
  const fields = [p.firstName, p.lastName, p.email, p.phone, p.location,
    p.institution, p.degreeLevel, p.fieldOfStudy,
    p.techSkills?.length, p.industries?.length, p.careerGoal];
  const filled = fields.filter(Boolean).length;
  const pct = Math.round((filled / fields.length) * 100);
  document.getElementById('completionPct').textContent = `${pct}%`;
  setTimeout(() => { document.getElementById('completionBar').style.width = `${pct}%`; }, 100);

  // Personal fields
  document.getElementById('personalFields').innerHTML = fieldRows([
    ['Email',    p.email],
    ['Phone',    p.phone],
    ['Location', p.location],
    ['LinkedIn', p.linkedin ? `<a href="${esc(p.linkedin)}" target="_blank" style="color:var(--accent2);">View Profile</a>` : '—'],
  ]);

  // Education fields
  document.getElementById('educationFields').innerHTML = fieldRows([
    ['Institution',    p.institution],
    ['Degree',         `${p.degreeLevel || ''} in ${p.fieldOfStudy || ''}`],
    ['Graduation',     p.graduationYear],
    ['Classification', p.classification],
    ['Certifications', (p.certifications || []).join(', ') || '—'],
    ['Experience',     expLabel(p.yearsExp)],
  ]);

  // Skills
  renderPills('techSkillPills', p.techSkills || [], 'pill-tech');
  renderPills('softSkillPills', p.softSkills || [], 'pill-soft');
  renderPills('langPills',      p.languages  || [], 'pill-lang');

  // Preferences
  document.getElementById('preferenceFields').innerHTML = fieldRows([
    ['Industries',  (p.industries || []).join(', ') || '—'],
    ['Job Types',   (p.jobTypes   || []).join(', ') || '—'],
    ['Location',    p.workLocation || '—'],
    ['Salary',      p.salaryRange  || 'Not specified'],
    ['Career Goal', p.careerGoal   || '—'],
  ]);
}

function fieldRows(pairs) {
  return pairs.map(([k, v]) => `
    <div class="profile-field">
      <span class="field-key">${k}</span>
      <span class="field-val">${v || '—'}</span>
    </div>`).join('');
}

function renderPills(containerId, items, cls) {
  document.getElementById(containerId).innerHTML =
    items.length
      ? items.map(t => `<span class="skill-pill ${cls}">${esc(t)}</span>`).join('')
      : '<span style="color:var(--text-muted);font-size:0.85rem;">None added</span>';
}

function esc(s) {
  return String(s).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

function expLabel(val) {
  const map = { '0':'Fresh Graduate', '1':'< 1 year', '2':'1–2 years', '3':'3–5 years', '5':'5+ years' };
  return map[val] ?? (val || '—');
}
