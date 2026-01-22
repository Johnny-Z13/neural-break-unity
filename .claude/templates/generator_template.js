/**
 * Algorithmic Art Generator Template
 * 
 * This file demonstrates p5.js best practices and code structure principles.
 * Use these patterns to build unique algorithms - DO NOT copy this directly.
 * Embed your algorithm inline in the HTML artifact.
 * 
 * Key Principles:
 * 1. Seeded randomness for reproducibility
 * 2. Parameterized everything
 * 3. Clean separation of concerns
 * 4. Performance-conscious patterns
 */

// ============================================
// PARAMETERS
// Define all tunable values in one place
// ============================================

const params = {
    // Always include seed
    seed: 12345,
    
    // Quantities
    particleCount: 1000,
    layerCount: 3,
    
    // Scales
    noiseScale: 0.005,
    speedScale: 2.0,
    sizeScale: 1.0,
    
    // Probabilities
    branchProbability: 0.15,
    fadeChance: 0.05,
    
    // Ratios
    aspectRatio: 1.0,
    goldenRatio: 1.618,
    
    // Thresholds
    minDistance: 5,
    maxIterations: 1000,
    
    // Colors
    backgroundColor: '#0a0a0f',
    primaryColor: '#c96442',
    secondaryColor: '#4a90a4'
};


// ============================================
// SEEDED RANDOMNESS
// Always use seeds for reproducibility
// ============================================

function initializeRandomness(seed) {
    randomSeed(seed);
    noiseSeed(seed);
}

// Custom seeded random helpers
function seededRandom(min = 0, max = 1) {
    return random(min, max);
}

function seededGaussian(mean = 0, sd = 1) {
    return randomGaussian(mean, sd);
}

function seededChoice(array) {
    return array[floor(random(array.length))];
}


// ============================================
// PARTICLE CLASS
// Example of a well-structured entity
// ============================================

class Particle {
    constructor(x, y, params) {
        this.x = x;
        this.y = y;
        this.px = x; // Previous position for trails
        this.py = y;
        
        this.vx = 0;
        this.vy = 0;
        
        this.params = params;
        this.age = 0;
        this.maxAge = random(100, 500);
        this.dead = false;
    }
    
    update(forceField) {
        // Store previous position
        this.px = this.x;
        this.py = this.y;
        
        // Get force from field
        const force = forceField.getForce(this.x, this.y);
        
        // Apply force with damping
        this.vx = this.vx * 0.95 + force.x * this.params.speedScale;
        this.vy = this.vy * 0.95 + force.y * this.params.speedScale;
        
        // Update position
        this.x += this.vx;
        this.y += this.vy;
        
        // Age
        this.age++;
        if (this.age > this.maxAge) {
            this.dead = true;
        }
        
        // Boundary handling
        this.handleBoundaries();
    }
    
    handleBoundaries() {
        // Wrap around
        if (this.x < 0) this.x = width;
        if (this.x > width) this.x = 0;
        if (this.y < 0) this.y = height;
        if (this.y > height) this.y = 0;
    }
    
    display(col) {
        const alpha = map(this.age, 0, this.maxAge, 255, 0);
        stroke(red(col), green(col), blue(col), alpha * 0.1);
        line(this.px, this.py, this.x, this.y);
    }
    
    isDead() {
        return this.dead;
    }
}


// ============================================
// FORCE FIELD CLASS
// Example of a vector field implementation
// ============================================

class ForceField {
    constructor(params) {
        this.params = params;
        this.time = 0;
    }
    
    getForce(x, y) {
        // Layered noise for complexity
        let angle = 0;
        let amplitude = 1;
        
        for (let i = 0; i < 3; i++) {
            const scale = this.params.noiseScale * pow(2, i);
            angle += noise(x * scale, y * scale, this.time) * TWO_PI * 2 * amplitude;
            amplitude *= 0.5;
        }
        
        return {
            x: cos(angle),
            y: sin(angle)
        };
    }
    
    update(dt = 0.01) {
        this.time += dt;
    }
}


// ============================================
// COLOR UTILITIES
// Thoughtful color handling
// ============================================

function createPalette(baseColor, count = 5) {
    const palette = [];
    const base = color(baseColor);
    
    for (let i = 0; i < count; i++) {
        const h = (hue(base) + i * (360 / count)) % 360;
        const s = saturation(base) * random(0.8, 1.2);
        const b = brightness(base) * random(0.8, 1.2);
        palette.push(color(h, s, b));
    }
    
    return palette;
}

function lerpPalette(palette, t) {
    const scaledT = t * (palette.length - 1);
    const i = floor(scaledT);
    const f = scaledT - i;
    
    if (i >= palette.length - 1) return palette[palette.length - 1];
    return lerpColor(palette[i], palette[i + 1], f);
}


// ============================================
// GEOMETRY UTILITIES
// Common mathematical operations
// ============================================

function goldenAngle() {
    return TWO_PI / (1 + (1 + sqrt(5)) / 2);
}

function fibonacci(n) {
    if (n <= 1) return n;
    let a = 0, b = 1;
    for (let i = 2; i <= n; i++) {
        [a, b] = [b, a + b];
    }
    return b;
}

function spiralPoint(index, scale = 1) {
    const angle = index * goldenAngle();
    const radius = scale * sqrt(index);
    return {
        x: cos(angle) * radius,
        y: sin(angle) * radius
    };
}

function polarToCartesian(r, theta) {
    return {
        x: r * cos(theta),
        y: r * sin(theta)
    };
}


// ============================================
// MAIN SKETCH STRUCTURE
// Standard p5.js lifecycle
// ============================================

let particles = [];
let forceField;
let palette;

function setup() {
    createCanvas(1200, 1200);
    colorMode(HSB, 360, 100, 100, 255);
    
    // Initialize randomness
    initializeRandomness(params.seed);
    
    // Create systems
    forceField = new ForceField(params);
    palette = createPalette(params.primaryColor);
    
    // Initialize particles
    for (let i = 0; i < params.particleCount; i++) {
        particles.push(new Particle(
            random(width),
            random(height),
            params
        ));
    }
    
    // Background
    background(params.backgroundColor);
}

function draw() {
    // Update and display particles
    for (let p of particles) {
        if (!p.isDead()) {
            p.update(forceField);
            p.display(seededChoice(palette));
        }
    }
    
    // Remove dead particles
    particles = particles.filter(p => !p.isDead());
    
    // Spawn new particles to maintain count
    while (particles.length < params.particleCount * 0.5) {
        particles.push(new Particle(
            random(width),
            random(height),
            params
        ));
    }
    
    // Stop after max iterations for static output
    if (frameCount > params.maxIterations) {
        noLoop();
    }
}


// ============================================
// PATTERN EXAMPLES (for reference only)
// Use as inspiration, not copy-paste
// ============================================

/*
FLOW FIELDS:
- Use noise() to generate angles
- Particles follow vector field
- Trails accumulate over time
- Layer multiple noise scales

RECURSIVE STRUCTURES:
- L-systems for branching
- Fractal subdivision
- Self-similar patterns
- Depth-limited recursion

CIRCLE PACKING:
- Iterative placement
- Collision detection
- Relaxation algorithms
- Size distribution

VORONOI / DELAUNAY:
- Point distribution
- Cell generation
- Edge drawing
- Lloyd relaxation

REACTION-DIFFUSION:
- Gray-Scott model
- Feed/kill rates
- Laplacian calculation
- Pattern emergence

WAVE INTERFERENCE:
- Multiple wave sources
- Phase relationships
- Constructive/destructive
- Standing waves
*/


// ============================================
// PERFORMANCE TIPS
// ============================================

/*
1. Avoid per-frame allocations
   - Reuse objects
   - Pool particles
   - Pre-calculate constants

2. Use typed arrays for heavy computation
   - Float32Array for coordinates
   - Uint8Array for pixel data

3. Batch draw calls
   - beginShape/endShape
   - Pre-render to graphics buffer

4. Limit active particles
   - Remove dead particles
   - Cap maximum count

5. Use noLoop() for static art
   - Run algorithm to completion
   - Display final frame only
*/
