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
    '^\\.\\./js/(.*)$': '<rootDir>/src/Neba.Website.Server/wwwroot/js/$1',
  },
  transform: {
    '^.+\\.js$': 'babel-jest',
  },
};
