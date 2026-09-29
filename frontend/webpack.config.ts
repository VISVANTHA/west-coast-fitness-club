import path from 'node:path';
import HtmlWebpackPlugin from 'html-webpack-plugin';
import MiniCssExtractPlugin from 'mini-css-extract-plugin';
import type { Configuration } from 'webpack';
import type { Configuration as DevServerConfiguration } from 'webpack-dev-server';

interface WebpackArgv {
  mode?: 'development' | 'production' | 'none';
}

const config = (_env: unknown, argv: WebpackArgv): Configuration & { devServer?: DevServerConfiguration } => {
  const production = argv.mode === 'production';

  return {
    entry: './src/main.tsx',
    output: {
      path: path.resolve(__dirname, 'dist'),
      filename: production ? 'assets/[name].[contenthash:8].js' : 'assets/[name].js',
      publicPath: '/',
      clean: true,
    },
    devtool: production ? 'hidden-source-map' : 'eval-cheap-module-source-map',
    resolve: {
      extensions: ['.tsx', '.ts', '.js'],
    },
    module: {
      rules: [
        {
          test: /\.tsx?$/,
          exclude: /node_modules/,
          use: {
            loader: 'ts-loader',
            // Type checking is done by `tsc --build` (see the build script), as CRA did with ForkTsChecker.
            options: { transpileOnly: true, configFile: path.resolve(__dirname, 'tsconfig.app.json') },
          },
        },
        {
          test: /\.css$/,
          use: [production ? MiniCssExtractPlugin.loader : 'style-loader', 'css-loader'],
        },
      ],
    },
    plugins: [
      new HtmlWebpackPlugin({ template: './public/index.html' }),
      ...(production ? [new MiniCssExtractPlugin({ filename: 'assets/[name].[contenthash:8].css' })] : []),
    ],
    performance: { hints: false },
    devServer: {
      port: 5173,
      historyApiFallback: true,
      proxy: [{ context: ['/api'], target: 'http://localhost:5080', changeOrigin: true }],
    },
  };
};

export default config;
