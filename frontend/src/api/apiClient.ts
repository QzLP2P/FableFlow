import axios from 'axios';
import type {
  AdventureDto,
  AdventureHistoryDto,
  MakeChoiceRequest,
  StartAdventureRequest,
  ThemeDto,
} from './types';

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5080';

export const apiClient = axios.create({
  baseURL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export async function getThemes(): Promise<ThemeDto[]> {
  const { data } = await apiClient.get<ThemeDto[]>('/api/themes');
  return data;
}

export async function startAdventure(request: StartAdventureRequest): Promise<AdventureDto> {
  const { data } = await apiClient.post<AdventureDto>('/api/adventures', request);
  return data;
}

export async function getAdventure(adventureId: string): Promise<AdventureDto> {
  const { data } = await apiClient.get<AdventureDto>(`/api/adventures/${adventureId}`);
  return data;
}

export async function makeChoice(
  adventureId: string,
  request: MakeChoiceRequest,
): Promise<AdventureDto> {
  const { data } = await apiClient.post<AdventureDto>(
    `/api/adventures/${adventureId}/choices`,
    request,
  );
  return data;
}

export async function getAdventureHistory(adventureId: string): Promise<AdventureHistoryDto> {
  const { data } = await apiClient.get<AdventureHistoryDto>(`/api/adventures/${adventureId}/history`);
  return data;
}
