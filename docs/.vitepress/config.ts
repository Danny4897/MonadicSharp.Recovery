import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'MonadicSharp.Recovery',
  description: 'Self-healing extension for MonadicSharp — RescueAsync and StartFixBranchAsync for Railway-Oriented error recovery.',
  base: '/MonadicSharp.Recovery/',
  cleanUrls: true,
  ignoreDeadLinks: true,

  head: [
    ['meta', { property: 'og:type', content: 'website' }],
    ['meta', { name: 'twitter:card', content: 'summary' }],
  ],

  themeConfig: {
    logo: '/logo.svg',
    siteTitle: 'MonadicSharp.Recovery',

    nav: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'The Three Lanes', link: '/three-lanes' },
        ],
      },
      {
        text: 'API',
        items: [
          { text: 'RescueAsync', link: '/api/rescue-async' },
          { text: 'StartFixBranchAsync', link: '/api/start-fix-branch-async' },
          { text: 'IRecoveryTelemetry', link: '/api/telemetry' },
        ],
      },
      {
        text: 'Ecosystem',
        items: [
          {
            text: 'Core',
            items: [
              { text: 'MonadicSharp', link: 'https://danny4897.github.io/MonadicSharp/' },
              { text: 'MonadicSharp.Framework', link: 'https://danny4897.github.io/MonadicSharp.Framework/' },
            ],
          },
          {
            text: 'Extensions',
            items: [
              { text: 'MonadicSharp.AI', link: 'https://danny4897.github.io/MonadicSharp.AI/' },
              { text: 'MonadicSharp.Recovery', link: 'https://danny4897.github.io/MonadicSharp.Recovery/' },
              { text: 'MonadicSharp.Azure', link: 'https://danny4897.github.io/MonadicSharp.Azure/' },
              { text: 'MonadicSharp.DI', link: 'https://danny4897.github.io/MonadicSharp.DI/' },
            ],
          },
          {
            text: 'Tooling',
            items: [
              { text: 'MonadicLeaf', link: 'https://danny4897.github.io/MonadicLeaf/' },
              { text: 'MonadicSharp × OpenCode', link: 'https://danny4897.github.io/MonadicSharp-OpenCode/' },
              { text: 'AgentScope', link: 'https://danny4897.github.io/AgentScope/' },
            ],
          },
        ],
      },
    ],

    sidebar: {
      '/': [
        {
          text: 'Guide',
          items: [
            { text: 'Getting Started', link: '/getting-started' },
            { text: 'The Three Lanes', link: '/three-lanes' },
          ],
        },
        {
          text: 'API Reference',
          items: [
            { text: 'RescueAsync', link: '/api/rescue-async' },
            { text: 'StartFixBranchAsync', link: '/api/start-fix-branch-async' },
            { text: 'IRecoveryTelemetry', link: '/api/telemetry' },
          ],
        },
        {
          text: 'Ecosystem',
          collapsed: true,
          items: [
            { text: 'MonadicSharp ↗', link: 'https://danny4897.github.io/MonadicSharp/' },
            { text: 'MonadicSharp.Framework ↗', link: 'https://danny4897.github.io/MonadicSharp.Framework/' },
            { text: 'MonadicSharp.AI ↗', link: 'https://danny4897.github.io/MonadicSharp.AI/' },
            { text: 'MonadicSharp.Recovery ↗', link: 'https://danny4897.github.io/MonadicSharp.Recovery/' },
            { text: 'MonadicSharp.Azure ↗', link: 'https://danny4897.github.io/MonadicSharp.Azure/' },
            { text: 'MonadicSharp.DI ↗', link: 'https://danny4897.github.io/MonadicSharp.DI/' },
            { text: 'MonadicLeaf ↗', link: 'https://danny4897.github.io/MonadicLeaf/' },
            { text: 'MonadicSharp × OpenCode ↗', link: 'https://danny4897.github.io/MonadicSharp-OpenCode/' },
            { text: 'AgentScope ↗', link: 'https://danny4897.github.io/AgentScope/' },
          ],
        },
      ],
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/Danny4897/MonadicSharp.Recovery' },
    ],

    search: { provider: 'local' },

    editLink: {
      pattern: 'https://github.com/Danny4897/MonadicSharp.Recovery/edit/main/docs/:path',
      text: 'Edit this page on GitHub',
    },


    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2024–2026 Danny4897',
    },

    outline: { level: [2, 3], label: 'On this page' },
  },

  markdown: {
    theme: { light: 'github-light', dark: 'one-dark-pro' },
  },
})
