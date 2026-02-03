export default {
  displayName: 'Neba.Website',
  testEnvironment: 'jsdom',
  setupFilesAfterEnv: ['<rootDir>/jest.setup.js'],
  testMatch: ['**/*.tests.js'],
  collectCoverageFrom: [
    'src/**/*.js',
    '!src/**/*.tests.js',
    '!src/**/bin/**',
    '!src/**/obj/**',
  ],
  coveragePathIgnorePatterns: [
    '/node_modules/',
    '/bin/',
    '/obj/',
  ],
  moduleNameMapper: {
    '\\.(css|less|scss|sass)$': 'identity-obj-proxy',
  },
  transform: {
    '^.+\\.js$': 'babel-jest',
  },
};
