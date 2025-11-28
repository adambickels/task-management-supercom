import { useEffect, useState } from 'react';
import {
  Container,
  Typography,
  Box,
  Button,
  Alert,
  CircularProgress,
} from '@mui/material';
import { Add as AddIcon } from '@mui/icons-material';
import TaskList from './components/TaskList';
import TaskForm from './components/TaskForm';
import type { TaskItem } from './types';
import { useAppDispatch, useAppSelector } from './store/hooks';
import { loadTasks, loadTags, deleteTask, clearError } from './store/taskSlice';
import styles from './App.module.scss';

const App = () => {
  const [open, setOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<TaskItem | null>(null);
  
  const dispatch = useAppDispatch();
  const { tasks, tags, loading, error } = useAppSelector((state) => state.tasks);

  useEffect(() => {
    dispatch(loadTasks());
    dispatch(loadTags());
  }, [dispatch]);

  const handleOpen = (task?: TaskItem) => {
    if (task) {
      setEditingTask(task);
    } else {
      setEditingTask(null);
    }
    setOpen(true);
  };

  const handleClose = () => {
    setOpen(false);
    setEditingTask(null);
  };

  const handleDelete = async (id: number) => {
    if (window.confirm('Are you sure you want to delete this task?')) {
      try {
        await dispatch(deleteTask(id)).unwrap();
      } catch (error) {
        // Log for developers (appears in console/monitoring)
        console.error('Failed to delete task:', error);
        // Error is also handled by Redux state and displayed in Alert component below
      }
    }
  };

  return (
    <Container maxWidth="lg" className={styles.container}>
      <Box className={styles.header}>
        <Box>
          <Typography variant="h3" component="h1" className={styles.title}>
            Task Management
          </Typography>
          <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mt: 0.5 }}>
            Full-Stack Developer Assignment for SUPERCOM • Adam Bickels
          </Typography>
        </Box>
        <Button
          variant="contained"
          startIcon={<AddIcon />}
          onClick={() => handleOpen()}
          size="large"
        >
          Add Task
        </Button>
      </Box>

      {error && (
        <Alert severity="error" onClose={() => dispatch(clearError())} className={styles.alert}>
          {error}
        </Alert>
      )}

      {loading && !tasks.length ? (
        <Box className={styles.loadingContainer}>
          <CircularProgress />
        </Box>
      ) : (
        <TaskList
          tasks={tasks}
          onEdit={handleOpen}
          onDelete={handleDelete}
        />
      )}

      <TaskForm
        open={open}
        onClose={handleClose}
        task={editingTask}
        tags={tags}
      />
    </Container>
  );
};

export default App;
