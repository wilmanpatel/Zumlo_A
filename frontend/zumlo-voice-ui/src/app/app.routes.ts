
// import { Routes } from '@angular/router';
// import { ConversationComponent } from './features/conversation/conversation.component';
// import { SummaryComponent } from './features/summary/summary.component';

// export const routes: Routes = [
//   { path: '', component: ConversationComponent },
//   { path: 'summary/:id', component: SummaryComponent }
// ];



import { convertToParamMap, Routes } from '@angular/router';
import { TestComponent } from './test.component';
import { ConversationComponent } from './features/conversation/conversation.component';
import { SummaryComponent } from './features/summary/summary.component';

export const routes: Routes = [
    { path: '', component: ConversationComponent },

    { path: 'summary/:id', component: SummaryComponent },
    { path: '**', redirectTo: '' }

];
