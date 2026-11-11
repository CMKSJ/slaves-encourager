import { render } from 'preact';
import { App } from './app';
import { initialize, startTicker } from './state/store';
import './styles/base.css';
import './styles/themes.css';
import './styles/flip.css';

initialize();
startTicker();
render(<App />, document.getElementById('app')!);
